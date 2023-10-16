
using System.Text.Json;
using RawDealView;
using RawDealView.Options;

namespace RawDeal
{
    
    public class GameEndException : Exception
    {
        public string Winner { get; private set; }

        public GameEndException(string winner)
        {
            Winner = winner;
        }
    }
    public class Card
    {
        public string Title { get; set; }
        public List<string> Types { get; set; }
        public List<string> Subtypes { get; set; }
        public string Fortitude { get; set; }
        public string Damage { get; set; }
        public string StunValue { get; set; }
        public string CardEffect { get; set; }
    }
    
    public class PlayerData
    {
        public List<string> Hand { get; set; }
        public List<string> Deck { get; set; }
        public List<string> RingArea { get; set; }
        public string SuperStarName { get; set; }
        public int Fortitude { get; set; }

        public PlayerData(List<string> hand, List<string> deck, List<string> ringArea, string superStarName, int fortitude)
        {
            Hand = hand;
            Deck = deck;
            RingArea = ringArea;
            SuperStarName = superStarName;
            Fortitude = fortitude;
        }
    }

    public class Superstar
    {
        public string Name { get; set; }
        public string Logo { get; set; }
        public int HandSize { get; set; }
        public int SuperstarValue { get; set; }
        public string SuperstarAbility { get; set; }
    }

    public class CardInfo : RawDealView.Formatters.IViewableCardInfo
    {
        public string Title { get; set; }
        public string Fortitude { get; set; }
        public string Damage { get; set; }
        public string StunValue { get; set; }
        public List<string> Types { get; set; }
        public List<string> Subtypes { get; set; }
        public string CardEffect { get; set; }

        public CardInfo(string title, string fortitude, string damage, string stunValue, List<string> types,
            List<string> subtypes, string cardEffect)
        {
            Title = title;
            Fortitude = fortitude;
            Damage = damage;
            StunValue = stunValue;
            Types = types;
            Subtypes = subtypes;
            CardEffect = cardEffect;
        }
    }
    
    public class PlayInfo : RawDealView.Formatters.IViewablePlayInfo
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public string Fortitude { get; set; }
        public string Damage { get; set; }
        public string StunValue { get; set; }
        public RawDealView.Formatters.IViewableCardInfo CardInfo { get; set; }
        public string PlayedAs { get; set; } 

        public PlayInfo(string title, string type, string fortitude, string damage, string stunValue, RawDealView.Formatters.IViewableCardInfo cardInfo, string playedAs)
        {
            Title = title;
            Type = type;
            Fortitude = fortitude;
            Damage = damage;
            StunValue = stunValue;
            CardInfo = cardInfo;
            PlayedAs = playedAs; 
        }
    }



    public class Game
    {
        private View _view;
        private string _deckFolder;
        private int startingPlayer = 0;
        private List<string> player1Hand = new List<string>();
        private List<string> player2Hand = new List<string>();
        private List<string> player1RingsidePile = new List<string>();
        private List<string> player2RingsidePile = new List<string>();
        private List<string> player1RingArea = new List<string>(); 
        private List<string> player2RingArea = new List<string>(); 
        private int player1FortitudeRating = 0;  
        private int player2FortitudeRating = 0;
        private List<Card> cardsInfo = null;
        private Superstar superstar1;
        private Superstar superstar2;
        private PlayerInfo player1;
        private PlayerInfo player2;
        bool abilityUsedThisTurn = false;

        
        public Game(View view, string deckFolder)
        {
            _view = view;
            _deckFolder = deckFolder;
        }

        public void Play()
        {
            try
            {
                List<string> player1Deck = LoadAndValidateDeck(out var superstarName1);
                if (player1Deck == null) return;

                List<string> player2Deck = LoadAndValidateDeck(out var superstarName2);
                if (player2Deck == null) return;

                InitializePlayerHands(superstarName1, player1Deck, 1);
                InitializePlayerHands(superstarName2, player2Deck, 2);
                DecideStartingPlayer(superstarName1, superstarName2);


                List<string> LoadAndValidateDeck(out string superstarName)
                {
                    string deckPath = _view.AskUserToSelectDeck(_deckFolder);
                    List<string> deck = LoadDeckFromFile(deckPath);
                    superstarName = ExtractSuperstarName(deck);

                    if (!IsDeckValid(deck, superstarName))
                    {
                        _view.SayThatDeckIsInvalid();
                        return null;
                    }

                    return deck;
                }

                string ExtractSuperstarName(List<string> deck)
                {
                    string name = deck[0].Replace(" (Superstar Card)", "");
                    deck.RemoveAt(0);
                    return name;
                }

                bool IsDeckValid(List<string> deck, string superstarName)
                {
                    string cardsPath = Path.Combine("data", "cards.json");
                    cardsInfo = LoadCardsInfo(cardsPath);


                    string superstarPath = Path.Combine("data", "superstar.json");
                    List<Superstar> superstarInfo = LoadSuperstarInfo(superstarPath);

                    return IsDeckCompletelyValid(deck, cardsInfo, superstarInfo, superstarName);
                }


                void InitializePlayerHands(string superstarName, List<string> deck, int playerNumber)
                {
                    var superstar = FindSuperstar(superstarName);
                    if (superstar == null) return;

                    var hand = DeterminePlayerHand(playerNumber);
                    PopulateHandWithCards(superstar, hand, deck);
                }

                List<string> DeterminePlayerHand(int playerNumber)
                {
                    var hand = (playerNumber == 1) ? player1Hand : player2Hand;
                    hand.Clear();
                    return hand;
                }

                void PopulateHandWithCards(Superstar superstar, List<string> hand, List<string> deck)
                {
                    int cardsToAdd = Math.Min(superstar.HandSize, deck.Count);
                    AddCardsToHand(hand, deck, cardsToAdd);
                }
                

                Superstar FindSuperstar(string superstarName)
                {
                    var superstarInfo = LoadSuperstarInfo(Path.Combine("data", "superstar.json"));
                    return superstarInfo.FirstOrDefault(s => s.Name == superstarName);
                }


                void AddCardsToHand(List<string> hand, List<string> deck, int cardsToAdd)
                {
                    hand.AddRange(deck.GetRange(deck.Count - cardsToAdd, cardsToAdd));
                    hand.Reverse();
                    deck.RemoveRange(deck.Count - cardsToAdd, cardsToAdd);
                }


                void DecideStartingPlayer(string superstarName1, string superstarName2)
                {
                    LoadSuperstars(superstarName1, superstarName2);

                    DetermineStartingPlayerAndBeginActions();
                }

                void LoadSuperstars(string superstarName1, string superstarName2)
                {
                    var superstarInfo = LoadSuperstarInfo(Path.Combine("data", "superstar.json"));
                    superstar1 = superstarInfo.FirstOrDefault(s => s.Name == superstarName1);
                    superstar2 = superstarInfo.FirstOrDefault(s => s.Name == superstarName2);
                }

                void DetermineStartingPlayerAndBeginActions()
                {
                    if (superstar1.SuperstarValue >= superstar2.SuperstarValue)
                    {
                        startingPlayer = 1;
                        HandlePlayerActions(1);
                    }
                    else
                    {
                        startingPlayer = 2;
                        HandlePlayerActions(2);
                    }
                }


                void HandlePlayerActions(int turno)
                {
                    InitializeTurnStatus();

                    PlayerInfo player1 = CreatePlayerInfo(superstar1.Name, player1FortitudeRating, player1Hand.Count, player1Deck.Count);
                    PlayerInfo player2 = CreatePlayerInfo(superstar2.Name, player2FortitudeRating, player2Hand.Count, player2Deck.Count);
                    ExecuteTurnBasedActions(turno, player1, player2);

                    HandleContinuousActions(turno);
                }

                void InitializeTurnStatus()
                {
                    abilityUsedThisTurn = false;
                }

                PlayerInfo CreatePlayerInfo(string name, int fortitude, int handCount, int deckCount)
                {
                    return new PlayerInfo(name, fortitude, handCount, deckCount);
                }

                void ExecuteTurnBasedActions(int turno, PlayerInfo player1, PlayerInfo player2)
                {
                    AnnounceTurnBegin(turno);
                    UseSpecialAbilities(turno);
                    if (turno == 1) HandleTurn(player1, player2, turno);
                    else HandleTurn(player2, player1, turno);
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }

                void AnnounceTurnBegin(int turno)
                {
                    _view.SayThatATurnBegins(turno == 1 ? superstarName1 : superstarName2);
                }

                void HandleContinuousActions(int turno)
                {
                    string currentPlayer = DetermineCurrentPlayer(turno);
                    ExecuteActionsUntilGiveUp(currentPlayer, turno);
                    CongratulateWinner(turno);
                }

                string DetermineCurrentPlayer(int turno)
                {
                    return (turno == 1) ? superstarName1 : superstarName2;
                }

                void ExecuteActionsUntilGiveUp(string currentPlayer, int turno)
                {
                    NextPlay action = DetermineAction(currentPlayer);
                    while (action != NextPlay.GiveUp)
                    {
                        HandleAction(action, currentPlayer, turno);
                        action = DetermineAction(currentPlayer);
                    }
                }


                void HandleTurn(PlayerInfo current, PlayerInfo opponent, int turno)
                {
                    (List<string> currentDeck, List<string> currentHand) = GetDeckAndHandBasedOnTurn(turno);

                    DrawCard(currentDeck, currentHand, turno);
                    HandleSpecialDraws(turno, currentDeck, currentHand);
    
                    UpdatePlayerInfo(out current, out opponent);
                    UpdatePlayerInfos();
                }

                (List<string>, List<string>) GetDeckAndHandBasedOnTurn(int turno)
                {
                    var currentDeck = (turno == 1) ? player1Deck : player2Deck;
                    var currentHand = (turno == 1) ? player1Hand : player2Hand;
                    return (currentDeck, currentHand);
                }

                void HandleSpecialDraws(int turno, List<string> currentDeck, List<string> currentHand)
                {
                    string currentPlayer = DetermineCurrentPlayer(turno);
                    if (currentPlayer == "MANKIND" && currentDeck.Count > 0)
                    {
                        DrawCard(currentDeck, currentHand, turno);
                    }
                }

                
                NextPlay DetermineAction(string currentPlayer)
                {
                    var currentHand = GetCurrentHand(currentPlayer);
                    if (CanUseAbility(currentPlayer, currentHand))
                        return _view.AskUserWhatToDoWhenUsingHisAbilityIsPossible();
                    return _view.AskUserWhatToDoWhenHeCannotUseHisAbility();
                }

                List<string> GetCurrentHand(string currentPlayer)
                {
                    return (currentPlayer == superstarName1) ? player1Hand : player2Hand;
                }

                bool CanUseAbility(string player, List<string> hand)
                {
                    return !abilityUsedThisTurn && EligibleForAbility(player, hand);
                }

                bool EligibleForAbility(string player, List<string> hand)
                {
                    return hand.Count > 0 && (player == "THE UNDERTAKER" && hand.Count >= 2 
                                              || player == "STONE COLD STEVE AUSTIN" 
                                              || player == "CHRIS JERICHO");
                }
                

                void HandleAction(NextPlay action, string currentPlayer, int turno)
                {
                    switch (action)
                    {
                        case NextPlay.UseAbility:
                            HandleUseAbilityAction(turno);
                            break;
                        case NextPlay.ShowCards:
                            HandleShowCardsAction(turno);
                            break;
                        case NextPlay.PlayCard:
                            HandlePlayCardAction(turno);
                            break;
                        case NextPlay.EndTurn:
                            HandleEndTurnAction(turno);
                            break;
                    }
                }

                void HandleUseAbilityAction(int turno)
                {
                    UseSpecialTurnAbilities(turno);
                }


                void HandleShowCardsAction(int turno)
                {
                    CardSet cardSetChoice = _view.AskUserWhatSetOfCardsHeWantsToSee();
                    switch (cardSetChoice)
                    {
                        case CardSet.Hand:
                            HandleShowHandCardsAction(turno);
                            break;
                        case CardSet.RingArea:
                            HandleShowRingAreaAction(turno);
                            break;
                        case CardSet.RingsidePile:
                            HandleShowRingsidePileAction(turno);
                            break;
                        case CardSet.OpponentsRingArea:
                            HandleShowOpponentRingAreaAction(turno);
                            break;
                        case CardSet.OpponentsRingsidePile:
                            HandleShowOpponentRingsidePileAction(turno);
                            break;
                    }
                }

                void HandleShowHandCardsAction(int turno)
                {
                    ShowPlayerHandCards(turno == 1 ? player1Hand : player2Hand);
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }

                void HandleShowRingAreaAction(int turno)
                {
                    ShowPlayerRingArea(turno == 1 ? player1RingArea : player2RingArea);
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }

                void HandleShowRingsidePileAction(int turno)
                {
                    ShowPlayerRingsidePile(turno == 1 ? player1RingsidePile : player2RingsidePile);
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }

                void HandleShowOpponentRingAreaAction(int turno)
                {
                    ShowPlayerRingArea(turno == 1 ? player2RingArea : player1RingArea);
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }

                void HandleShowOpponentRingsidePileAction(int turno)
                {
                    ShowPlayerRingsidePile(turno == 1 ? player2RingsidePile : player1RingsidePile);
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }

                
                void HandlePlayCardAction(int turno)
                {
                    PlayCardForPlayer(turno);
                    PostPlayUpdates(turno);
                }

                void PlayCardForPlayer(int turno)
                {
                    if (turno == 1) PlayCardForPlayer1();
                    else PlayCardForPlayer2();
                }

                void PostPlayUpdates(int turno)
                {
                    UpdatePlayerInfos();
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }


                void PlayCardForPlayer1()
                {
                    PlayCardAction(player1Hand, player1Deck, player1RingArea, player2Deck, player2RingsidePile,
                        cardsInfo, superstarName1, superstarName2, player1FortitudeRating, 1);
                }


                void PlayCardForPlayer2()
                {

                    PlayCardAction(player2Hand, player2Deck, player2RingArea, player1Deck, player1RingsidePile,
                        cardsInfo, superstarName2, superstarName1, player2FortitudeRating, 2);
                }


                void HandleEndTurnAction(int turno)
                {
                    CheckDeckStatus(turno);
    
                    int opponentTurn = (turno == 1) ? 2 : 1;
                    HandlePlayerActions(opponentTurn);
                }

                void CheckDeckStatus(int turno)
                {
                    if (IsDeckEmpty(turno)) 
                    {
                        CongratulateCorrectWinner(turno);
                    }
    
                    if (IsDeckEmpty((turno == 1) ? 2 : 1)) 
                    {
                        CongratulateCorrectWinner((turno == 1) ? 2 : 1);  
                    }
                }


                bool IsDeckEmpty(int turno)
                {
                    bool isEmpty = (turno == 1 && player1Deck.Count == 0) || (turno == 2 && player2Deck.Count == 0);
                    return isEmpty;
                }


                void ShowGameInfoBasedOnCurrentTurn(int turno)
                {
                    PlayerInfo p1 = CreatePlayerInfo(superstar1.Name, player1FortitudeRating, player1Hand.Count, player1Deck.Count);
                    PlayerInfo p2 = CreatePlayerInfo(superstar2.Name, player2FortitudeRating, player2Hand.Count, player2Deck.Count);
    
                    DisplayInfoByTurn(p1, p2, turno);
                }
                

                void DisplayInfoByTurn(PlayerInfo p1, PlayerInfo p2, int turno)
                {
                    if (turno == 1)
                    {
                        _view.ShowGameInfo(p1, p2);
                    }
                    else
                    {
                        _view.ShowGameInfo(p2, p1);
                    }
                }
                

                void UseSpecialAbilities(int turno)
                {
                    UseTheRockAbility(turno);
                    if ((turno == 1 && superstarName1.ToUpper() == "KANE") ||
                        (turno == 2 && superstarName2.ToUpper() == "KANE"))
                    {
                        ApplyKaneAbility(turno);
                    }
                }

                void UseSpecialTurnAbilities(int turno)
                {
                    UseUndertakerAbility(turno);
                    UseJerichoAbility(turno);
                    UseStoneColdAbility(turno);
                    
                    abilityUsedThisTurn = true;
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }

                
                void CongratulateWinner(int turno)
                {
                    string winner = (turno == 1) ? superstarName2 : superstarName1;
                    _view.CongratulateWinner(winner);
                    throw new GameEndException(winner);
                }
                
                
                void CongratulateCorrectWinner(int turnoWithoutCards)
                {
                    int winningTurn = (turnoWithoutCards == 1) ? 2 : 1;
                    string winner = (winningTurn == 1) ? superstarName1 : superstarName2;
                    _view.CongratulateWinner(winner);
                    throw new GameEndException(winner);
                }

                
                void DrawCard(List<string> playerDeck, List<string> playerHand, int turno)
                {

                    if (playerDeck.Any())
                    {
                        string drawnCard = playerDeck.Last();
                        playerDeck.RemoveAt(playerDeck.Count - 1);
                        playerHand.Add(drawnCard);
                    }
                }


                void PlayCardAction(List<string> playerHand, List<string> playerDeck,
                    List<string> playerRingArea, List<string> playerDeckOpponent,
                    List<string> ringSidePileOpponent, List<Card> cardsInfo, string superStarName,
                    string superStarNameOpponent, int playerFortitude, int turno)
                {
                    List<int> playableCardIndices = GetPlayableCardIndices(playerHand, cardsInfo, playerFortitude);
                    List<string> cardsToDisplay =
                        FormatPlayableCardsForDisplay(playableCardIndices, playerHand, cardsInfo);


                    int cardIndex = _view.AskUserToSelectAPlay(cardsToDisplay);
                    if (IsValidCardIndex(cardIndex, playableCardIndices))
                    {
                        ProcessSelectedCard(playableCardIndices[cardIndex], playerHand, playerRingArea,
                            playerDeckOpponent,
                            ringSidePileOpponent, cardsInfo, superStarName, superStarNameOpponent, turno);
                    }
                }


                List<int> GetPlayableCardIndices(List<string> playerHand, List<Card> cardsInfo, int playerFortitude)
                {
                    return playerHand.Select((cardName, index) => new { cardName, index })
                        .Where(x =>
                        {
                            var cardInfo = ConvertToCardInfo(x.cardName, cardsInfo);
                            return CardIsPlayable(cardInfo, playerFortitude);
                        })
                        .Select(x => x.index)
                        .ToList();
                }


                bool CardIsPlayable(RawDealView.Formatters.IViewableCardInfo cardInfo, int playerFortitude)
                {
                    if (!int.TryParse(cardInfo.Fortitude, out int cardFortitude))
                    {
                        return false;
                    }
                    return (cardInfo.Types.Contains("Maneuver") || cardInfo.Types.Contains("Action"))
                           && cardFortitude <= playerFortitude;
                }


                List<string> FormatPlayableCardsForDisplay(List<int> playableCardIndices, List<string> playerHand, List<Card> cardsInfo)
                {
                    return playableCardIndices
                        .Select(index => ConvertToCardInfo(playerHand[index], cardsInfo))
                        .Select(viewableCardInfo => FormatCardInfoForDisplay(viewableCardInfo))
                        .ToList();
                }

                bool IsValidCardIndex(int cardIndex, List<int> playableCardIndices)
                {
                    return cardIndex >= 0 && cardIndex < playableCardIndices.Count;
                }
                
                string FormatCardInfoForDisplay(RawDealView.Formatters.IViewableCardInfo viewableCardInfo)
                {
                    var playInfo = new PlayInfo(
                        viewableCardInfo.Title,
                        viewableCardInfo.Types[0],
                        viewableCardInfo.Fortitude,
                        viewableCardInfo.Damage,
                        viewableCardInfo.StunValue,
                        viewableCardInfo,
                        viewableCardInfo.Types[0].ToUpper()
                    );
                    return RawDealView.Formatters.Formatter.PlayToString(playInfo);
                }


                void ProcessSelectedCard(int cardIndex, List<string> playerHand, List<string> playerRingArea,
                    List<string> playerDeckOpponent, List<string> ringSidePileOpponent, List<Card> cardsInfo,
                    string superStarName, string superStarNameOpponent, int turno)
                {
                    string cardName = playerHand[cardIndex];
                    DisplayPlayerAction(superStarName, cardName, cardsInfo);

                    playerHand.RemoveAt(cardIndex);
                    playerRingArea.Add(cardName);

                    ApplyCardEffects(cardName, cardsInfo, superStarNameOpponent, playerDeckOpponent,
                        ringSidePileOpponent, turno);
                }


                void DisplayPlayerAction(string superStarName, string cardName, List<Card> cardsInfo)
                {
                    var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                    var playInfo = new PlayInfo(
                        cardInfo.Title,
                        cardInfo.Types[0],
                        cardInfo.Fortitude,
                        cardInfo.Damage,
                        cardInfo.StunValue,
                        cardInfo,
                        cardInfo.Types[0].ToUpper());

                    _view.SayThatPlayerIsTryingToPlayThisCard(superStarName,
                        RawDealView.Formatters.Formatter.PlayToString(playInfo));
                    _view.SayThatPlayerSuccessfullyPlayedACard();
                }


                void ApplyCardEffects(string cardName, List<Card> cardsInfo, string superStarNameOpponent,
                    List<string> playerDeckOpponent, List<string> ringSidePileOpponent, int turno)
                {
                    int damage = CalculateDamage(cardName, cardsInfo, superStarNameOpponent);
                    if (damage <= 0) return;

                    _view.SayThatSuperstarWillTakeSomeDamage(superStarNameOpponent, damage);
                    if (superStarNameOpponent == "MANKIND")
                    {
                        IncreaseFortitude(damage + 1, turno);
                        OverturnCardsForDamage(damage, playerDeckOpponent, ringSidePileOpponent, cardsInfo,
                            superStarNameOpponent, turno);
                        return;
                    }

                    IncreaseFortitude(damage, turno);

                    OverturnCardsForDamage(damage, playerDeckOpponent, ringSidePileOpponent, cardsInfo,
                        superStarNameOpponent, turno);
                }


                void OverturnCardsForDamage(int damage, List<string> playerDeckOpponent,
                    List<string> ringSidePileOpponent,
                    List<Card> cardsInfo, string superStarName,
                    int turno)
                {
                    for (int i = 0; i < damage; i++)
                    {
                        if (playerDeckOpponent.Count == 0)
                        {
                            int opponentTurn = (turno == 1) ? 2 : 1;
                            CongratulateWinner(opponentTurn);
                            throw new GameEndException(superStarName);
                        }


                        string overturnedCardName = playerDeckOpponent.Last();
                        playerDeckOpponent.RemoveAt(playerDeckOpponent.Count - 1);
                        ringSidePileOpponent.Add(overturnedCardName);

                        var overturnedCardInfo = ConvertToCardInfo(overturnedCardName, cardsInfo);
                        string cardInfoString = RawDealView.Formatters.Formatter.CardToString(overturnedCardInfo);

                        _view.ShowCardOverturnByTakingDamage(cardInfoString, i + 1, damage);
                    }
                }


                int CalculateDamage(string cardName, List<Card> cardsInfo, string targetSuperstarName)
                {
                    var cardInfo = cardsInfo.FirstOrDefault(card => card.Title == cardName);

                    if (cardInfo != null && int.TryParse(cardInfo.Damage, out int damageValue))
                    {
                        // Si el objetivo es Mankind, reducir el daño en 1.
                        if (targetSuperstarName == "MANKIND" && damageValue > 0)
                        {
                            Console.WriteLine("ENTRE MADNKINDS");
                            damageValue -= 1;
                            Console.WriteLine(damageValue);

                        }

                        return damageValue;
                    }

                    return 0;
                }


                void IncreaseFortitude(int fortitudeValue, int playerId)
                {
                    if (playerId == 1)
                    {
                        player1FortitudeRating += fortitudeValue;
                    }
                    else if (playerId == 2)
                    {
                        player2FortitudeRating += fortitudeValue;
                    }
                }


                void ShowPlayerHandCards(List<string> playerHand)
                {

                    // Tomar las cartas 
                    var cardsToShow = playerHand;

                    // Convertir los nombres de las cartas a objetos CardInfo
                    List<RawDealView.Formatters.IViewableCardInfo> viewableCardsInfo =
                        cardsToShow.Select(cardName =>
                        {
                            var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                            return cardInfo;
                        }).Where(cardInfo => cardInfo != null).ToList();

                    // Acumular las cartas formateadas en una lista
                    List<string> cardsToDisplay = new List<string>();

                    for (int i = 0; i < viewableCardsInfo.Count; i++)
                    {
                        var cardInfo = viewableCardsInfo[i];
                        string formattedCard = RawDealView.Formatters.Formatter.CardToString(cardInfo);
                        cardsToDisplay.Add(formattedCard);
                    }

                    // Mostrar todas las cartas juntas
                    _view.ShowCards(cardsToDisplay);
                }


                void ShowPlayerRingArea(List<string> ringArea)
                {
                    // Convertir los nombres de las cartas a objetos CardInfo
                    List<RawDealView.Formatters.IViewableCardInfo> viewableCardsInfo =
                        ringArea.Select(cardName => ConvertToCardInfo(cardName, cardsInfo)).ToList();

                    // Acumular las cartas formateadas en una lista
                    List<string> cardsToDisplay = new List<string>();

                    for (int i = 0; i < viewableCardsInfo.Count; i++)
                    {
                        var cardInfo = viewableCardsInfo[i];
                        string formattedCard = RawDealView.Formatters.Formatter.CardToString(cardInfo);
                        cardsToDisplay.Add(formattedCard);
                    }

                    _view.ShowCards(cardsToDisplay);
                }


                void ShowPlayerRingsidePile(List<string> ringsidePile)
                {
                    // Convertir los nombres de las cartas a objetos CardInfo
                    List<RawDealView.Formatters.IViewableCardInfo> viewableCardsInfo =
                        ringsidePile.Select(cardName => ConvertToCardInfo(cardName, cardsInfo)).ToList();

                    // Acumular las cartas formateadas en una lista
                    List<string> cardsToDisplay = new List<string>();

                    for (int i = 0; i < viewableCardsInfo.Count; i++)
                    {
                        var cardInfo = viewableCardsInfo[i];
                        string formattedCard = RawDealView.Formatters.Formatter.CardToString(cardInfo);
                        cardsToDisplay.Add(formattedCard);
                    }

                    // Mostrar todas las cartas juntas
                    _view.ShowCards(cardsToDisplay);
                }


                RawDealView.Formatters.IViewableCardInfo ConvertToCardInfo(string cardName, List<Card> cardsInfoList)
                {

                    // Buscar la carta en la lista de información de cartas
                    var cardData = cardsInfoList.FirstOrDefault(c => c.Title == cardName);
                    if (cardData != null)
                    {
                        return new CardInfo(cardData.Title, cardData.Fortitude, cardData.Damage, cardData.StunValue,
                            cardData.Types, cardData.Subtypes, cardData.CardEffect);
                    }

                    return null; // o manejar el caso en que no se encuentra la carta
                }


                void UpdatePlayerInfo(out PlayerInfo player1, out PlayerInfo player2)
                {
                    player1 = new PlayerInfo(superstarName1, player1FortitudeRating,
                        player1Hand.Count,
                        player1Deck.Count);
                    player2 = new PlayerInfo(superstarName2, player2FortitudeRating,
                        player2Hand.Count,
                        player2Deck.Count);
                }


                void UpdatePlayerInfos()
                {
                    player1 = new PlayerInfo(superstar1.Name, player1FortitudeRating, player1Hand.Count,
                        player1Deck.Count);
                    player2 = new PlayerInfo(superstar2.Name, player2FortitudeRating, player2Hand.Count,
                        player2Deck.Count);
                }


                List<string> LoadDeckFromFile(string filePath)
                {
                    List<string> deck = new List<string>();

                    try
                    {
                        string[] lines = File.ReadAllLines(filePath);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            deck.Add(lines[i]);
                        }
                    }
                    catch (Exception ex)
                    {
                    }

                    return deck;
                }


                List<Card> LoadCardsInfo(string filePath)
                {
                    List<Card> cardsInfo = new List<Card>();

                    try
                    {
                        string json = File.ReadAllText(filePath);
                        cardsInfo = JsonSerializer.Deserialize<List<Card>>(json);
                    }
                    catch (Exception ex)
                    {
                    }

                    return cardsInfo;
                }


                List<Superstar> LoadSuperstarInfo(string filePath)
                {
                    List<Superstar> superstarInfo = new List<Superstar>();

                    try
                    {
                        string json = File.ReadAllText(filePath);
                        superstarInfo = JsonSerializer.Deserialize<List<Superstar>>(json);
                    }
                    catch (Exception ex)
                    {
                    }

                    return superstarInfo;
                }


                bool IsDeckCompletelyValid(List<string> deck, List<Card> cardsInfo, List<Superstar> superstarInfo,
                    string superstarName)
                {
                    int totalFortitude = 0;
                    HashSet<string> uniqueCardTitles = new HashSet<string>();
                    bool hasSetupCard = false;
                    bool hasHeelCard = false;
                    bool hasFaceCard = false;

                    Dictionary<string, int> cardCounts = new Dictionary<string, int>();

                    foreach (string cardTitle in deck)
                    {
                        Card card = cardsInfo.FirstOrDefault(c => c.Title == cardTitle);

                        if (card == null)
                        {
                            return false;
                        }

                        totalFortitude += int.Parse(card.Fortitude);

                        if (!cardCounts.ContainsKey(card.Title))
                        {
                            cardCounts[card.Title] = 1;
                        }
                        else
                        {
                            cardCounts[card.Title]++;
                        }

                        if (card.Subtypes.Contains("Unique"))
                        {
                            if (!card.Subtypes.Contains("SetUp"))
                            {
                                if (uniqueCardTitles.Contains(card.Title))
                                {
                                    return false;
                                }

                                uniqueCardTitles.Add(card.Title);
                            }
                        }

                        if (card.Subtypes.Contains("SetUp"))
                        {
                            hasSetupCard = true;
                        }

                        if (card.Subtypes.Contains("Heel"))
                        {
                            hasHeelCard = true;
                        }

                        if (card.Subtypes.Contains("Face"))
                        {
                            hasFaceCard = true;
                        }
                    }

                    foreach (var pair in cardCounts)
                    {
                        Card currentCard = cardsInfo.FirstOrDefault(c => c.Title == pair.Key);
                        if (pair.Value > 3 && (currentCard == null || !currentCard.Subtypes.Contains("SetUp")))
                        {
                            return false;
                        }
                    }

                    Superstar superstar = superstarInfo.FirstOrDefault(s => s.Name == superstarName);
                    if (deck.Count != 60 || (hasHeelCard && hasFaceCard) || superstar == null)
                    {
                        return false;
                    }

                    foreach (string cardTitle in deck)
                    {
                        Card card = cardsInfo.FirstOrDefault(c => c.Title == cardTitle);

                        if (card.Subtypes.Any(subtype =>
                                superstarInfo.Any(s => s.Logo == subtype) && subtype != superstar.Logo))
                        {
                            return false;
                        }
                    }

                    return true;

                }


                // Funcion para manejar la habilidad de Kane
                void ApplyKaneAbility(int turno)
                {
                    List<string> opponentPlayerDeck;
                    if (turno == 1)
                    {
                        _view.SayThatPlayerIsGoingToUseHisAbility("KANE", superstar1.SuperstarAbility);
                        _view.SayThatSuperstarWillTakeSomeDamage(superstarName2, 1);
                        opponentPlayerDeck = player2Deck;
                    }
                    else
                    {
                        _view.SayThatPlayerIsGoingToUseHisAbility("KANE", superstar2.SuperstarAbility);
                        _view.SayThatSuperstarWillTakeSomeDamage(superstarName1, 1);
                        opponentPlayerDeck = player1Deck;
                    }

                    if (opponentPlayerDeck.Any())
                    {
                        string overturnedCardName = opponentPlayerDeck.Last();
                        opponentPlayerDeck.RemoveAt(opponentPlayerDeck.Count - 1);
                        if (turno == 1)
                        {
                            player2RingsidePile.Add(overturnedCardName);
                        }
                        else
                        {
                            player1RingsidePile.Add(overturnedCardName);
                        }

                        var overturnedCardInfo = ConvertToCardInfo(overturnedCardName, cardsInfo);
                        string cardInfoString = RawDealView.Formatters.Formatter.CardToString(overturnedCardInfo);
                        _view.ShowCardOverturnByTakingDamage(cardInfoString, 1, 1);
                    }
                }


                void UseTheRockAbility(int turn)
                {

                    string currentPlayer = (turn == 1) ? superstar1.Name : superstar2.Name;
                    string superstarAbility = (turn == 1) ? superstar1.SuperstarAbility : superstar2.SuperstarAbility;
                    List<string> currentArsenal = (turn == 1) ? player1Deck : player2Deck;
                    List<string> currentRingSide = (turn == 1) ? player1RingsidePile : player2RingsidePile;

                    if (currentPlayer == "THE ROCK" && currentRingSide.Count() > 0)
                    {
                        bool wantsToUseAbility = _view.DoesPlayerWantToUseHisAbility("THE ROCK");

                        if (wantsToUseAbility)
                        {
                            List<string> formattedRingSide = currentRingSide.Select(cardName =>
                            {
                                var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                                return RawDealView.Formatters.Formatter.CardToString(cardInfo);
                            }).ToList();

                            _view.SayThatPlayerIsGoingToUseHisAbility("THE ROCK", superstarAbility);
                            int cardId = _view.AskPlayerToSelectCardsToRecover(currentPlayer, 1, formattedRingSide);

                            string selectedCard = currentRingSide[cardId]; // Corrigiendo el orden aquí
                            currentRingSide.RemoveAt(cardId);
                            currentArsenal.Insert(0, selectedCard); // Poner la carta al fondo del arsenal
                        }
                    }
                }


                void UseUndertakerAbility(int turn)
                {
                    string currentPlayer = (turn == 1) ? superstarName1 : superstarName2;
                    string superstarAbility = (turn == 1) ? superstar1.SuperstarAbility : superstar2.SuperstarAbility;
                    List<string> currentPlayerHand = (turn == 1) ? player1Hand : player2Hand;
                    List<string> currentPlayerRingside = (turn == 1) ? player1RingsidePile : player2RingsidePile;

                    if (currentPlayer == "THE UNDERTAKER" && currentPlayerHand.Count >= 2)
                    {
                        _view.SayThatPlayerIsGoingToUseHisAbility("THE UNDERTAKER", superstarAbility);

                        // descartar 2 cartas
                        for (int i = 0; i < 2; i++)
                        {
                            List<string> formattedHand = currentPlayerHand.Select(cardName =>
                            {
                                var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                                return RawDealView.Formatters.Formatter.CardToString(cardInfo);
                            }).ToList();

                            int cardIdToDiscard = _view.AskPlayerToSelectACardToDiscard(formattedHand,
                                "THE UNDERTAKER",
                                "THE UNDERTAKER", 2 - i);
                            string discardedCard = currentPlayerHand[cardIdToDiscard];
                            currentPlayerHand.RemoveAt(cardIdToDiscard);
                            currentPlayerRingside.Add(discardedCard);
                        }

                        List<string> formattedRingSide = currentPlayerRingside.Select(cardName =>
                        {
                            var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                            return RawDealView.Formatters.Formatter.CardToString(cardInfo);
                        }).ToList();

                        // elegir carta del ringside
                        int cardIdToRecover =
                            _view.AskPlayerToSelectCardsToPutInHisHand("THE UNDERTAKER", 1, formattedRingSide);
                        string recoveredCard = currentPlayerRingside[cardIdToRecover];
                        currentPlayerRingside.RemoveAt(cardIdToRecover);
                        currentPlayerHand.Add(recoveredCard);
                    }
                }

                void UseJerichoAbility(int turn)
                {
                    string currentPlayer = (turn == 1) ? superstarName1 : superstarName2;
                    string opponentPlayer = (turn == 1) ? superstarName2 : superstarName1;
                    string superstarAbility = (turn == 1) ? superstar1.SuperstarAbility : superstar2.SuperstarAbility;

                    List<string> currentPlayerHand = (turn == 1) ? player1Hand : player2Hand;
                    List<string> opponentPlayerHand = (turn == 1) ? player2Hand : player1Hand;

                    List<string> currentPlayerRingside = (turn == 1) ? player1RingsidePile : player2RingsidePile;
                    List<string> opponentPlayerRingside = (turn == 1) ? player2RingsidePile : player1RingsidePile;

                    if (currentPlayer == "CHRIS JERICHO" && currentPlayerHand.Count >= 1)
                    {
                        _view.SayThatPlayerIsGoingToUseHisAbility("CHRIS JERICHO", superstarAbility);

                        // Jericho descarta 1 carta
                        List<string> formattedHand = currentPlayerHand.Select(cardName =>
                        {
                            var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                            return RawDealView.Formatters.Formatter.CardToString(cardInfo);
                        }).ToList();

                        int cardIdToDiscard = _view.AskPlayerToSelectACardToDiscard(formattedHand,
                            "CHRIS JERICHO",
                            "CHRIS JERICHO", 1);
                        string discardedCard = currentPlayerHand[cardIdToDiscard];
                        currentPlayerHand.RemoveAt(cardIdToDiscard);
                        currentPlayerRingside.Add(discardedCard);

                        // El oponente descarta 1 carta
                        List<string> formattedOpponentHand = opponentPlayerHand.Select(cardName =>
                        {
                            var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                            return RawDealView.Formatters.Formatter.CardToString(cardInfo);
                        }).ToList();

                        int opponentCardIdToDiscard = _view.AskPlayerToSelectACardToDiscard(formattedOpponentHand,
                            opponentPlayer,
                            opponentPlayer, 1);
                        string opponentDiscardedCard = opponentPlayerHand[opponentCardIdToDiscard];
                        opponentPlayerHand.RemoveAt(opponentCardIdToDiscard);
                        opponentPlayerRingside.Add(opponentDiscardedCard);
                    }
                }

                void UseStoneColdAbility(int turno)
                {
                    string currentPlayer = (turno == 1) ? superstarName1 : superstarName2;
                    string superstarAbility = (turno == 1) ? superstar1.SuperstarAbility : superstar2.SuperstarAbility;
                    List<string> currentPlayerDeck = (turno == 1) ? player1Deck : player2Deck;
                    List<string> currentPlayerHand = (turno == 1) ? player1Hand : player2Hand;

                    if (currentPlayer == "STONE COLD STEVE AUSTIN" && currentPlayerDeck.Count > 0 && !abilityUsedThisTurn)
                    {
                        _view.SayThatPlayerIsGoingToUseHisAbility("STONE COLD STEVE AUSTIN", superstarAbility);

                        // Robar una carta del arsenal y agregarla a la mano
                        DrawCard(currentPlayerDeck, currentPlayerHand, turno);
                        _view.SayThatPlayerDrawCards("STONE COLD STEVE AUSTIN", 1);
                        
                        // Formatear la mano del jugador para mostrarla al mismo estilo que el Undertaker
                        List<string> formattedHand = currentPlayerHand.Select(cardName =>
                        {
                            var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                            return RawDealView.Formatters.Formatter.CardToString(cardInfo);
                        }).ToList();

                        // Seleccionar una carta de la mano para ponerla en la parte inferior del arsenal
                        int cardIdToReturn = _view.AskPlayerToReturnOneCardFromHisHandToHisArsenal(currentPlayer, formattedHand);
                        string returnedCard = currentPlayerHand[cardIdToReturn];
                        currentPlayerHand.RemoveAt(cardIdToReturn);
                        currentPlayerDeck.Insert(0, returnedCard); // Insertar al comienzo (parte inferior) del arsenal

                        abilityUsedThisTurn = true;
                    }
                }


            }



            catch (GameEndException exception)
            {
                Console.WriteLine("");
            }
        }
    }
}




        
        
