
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
    
    public class Player 
    {
        public string Name { get; set; }
        public List<string> Deck { get; set; } = new List<string>();
        public List<string> Hand { get; set; } = new List<string>();
        public List<string> RingArea { get; set; } = new List<string>();
        public List<string> RingsidePile { get; set; } = new List<string>();
        public int FortitudeRating { get; set; }
    }
    
    public struct PlayerState
    {
        public string Name;
        public string SuperstarAbility;
        public List<string> Hand;
        public List<string> Ringside;
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
        private int player1FortitudeRating;
        private int player2FortitudeRating;
        private bool usedJockeying;
        private bool wasDamageIncreased;
        private int JockeyingTurn;
        private int JockeyingEffect;
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

                    DrawCard(currentDeck, currentHand);
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
                        DrawCard(currentDeck, currentHand);
                    }
                }

                
                NextPlay DetermineAction(string currentPlayer)
                {
                    var currentHand = GetCurrentHand(currentPlayer);
                    var currentDeck = GetCurrentDeck(currentPlayer);
                    if (CanUseAbility(currentPlayer, currentHand, currentDeck))
                        return _view.AskUserWhatToDoWhenUsingHisAbilityIsPossible();
                    return _view.AskUserWhatToDoWhenHeCannotUseHisAbility();
                }

                
                List<string> GetCurrentHand(string currentPlayer)
                {
                    return (currentPlayer == superstarName1) ? player1Hand : player2Hand;
                }
                
                List<string> GetCurrentDeck(string currentPlayer)
                {
                    return (currentPlayer == superstarName1) ? player1Deck : player2Deck;
                }


                bool CanUseAbility(string player, List<string> hand, List<string> deck)
                {
                    return !abilityUsedThisTurn && EligibleForAbility(player, hand, deck);
                }

                bool EligibleForAbility(string player, List<string> hand, List<string> deck)
                {
                    Console.WriteLine("EL LARGO DEL DECK ESSSSSSSSSS");
                    Console.WriteLine(deck.Count);
                    return hand.Count > 0 && (player == "THE UNDERTAKER" && hand.Count >= 2 
                                              || player == "STONE COLD STEVE AUSTIN" && deck.Count > 0
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
                            JockeyingEffect=0;
                            JockeyingTurn=0;
                            usedJockeying=false;
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

                
                
                void PostPlayUpdates(int turno)
                {
                    UpdatePlayerInfos();
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }
                
                

                void HandleEndTurnAction(int turno)
                {
       
                    CheckDeckStatus(turno);
    
                    int opponentTurn = (turno == 1) ? 2 : 1;
                    HandlePlayerActions(opponentTurn);
                    Console.WriteLine("[DEBUG] Cambio de turno, reiniciando efecto Jockeying.");
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

                
                void DrawCard(List<string> playerDeck, List<string> playerHand)
                {
                    Console.WriteLine("MIRA COMO DRAWEA LA BACTERIA QLA");
                    if (playerDeck.Any())
                    {
                        string drawnCard = playerDeck.Last();
                        playerDeck.RemoveAt(playerDeck.Count - 1);
                        playerHand.Add(drawnCard);
                    }
                }
                
                
                Dictionary<string, object> CreateGameDataDictionary(int turno)
                {
                    Dictionary<string, object> gameData = new Dictionary<string, object>();
                    gameData["playerHand"] = (turno == 1) ? player1Hand : player2Hand;
                    gameData["playerDeck"] = (turno == 1) ? player1Deck : player2Deck;
                    gameData["playerRingArea"] = (turno == 1) ? player1RingArea : player2RingArea;
                    gameData["playerRingAreaOpponent"] = (turno == 1) ? player2RingArea : player1RingArea;
                    gameData["playerHandOpponent"] = (turno == 1) ? player2Hand : player1Hand;
                    gameData["playerRingSidePile"] = (turno == 1) ? player1RingsidePile : player2RingsidePile;
                    gameData["playerDeckOpponent"] = (turno == 1) ? player2Deck : player1Deck; 
                    gameData["ringSidePileOpponent"] = (turno == 1) ? player2RingsidePile : player1RingsidePile; 
                    gameData["cardsInfo"] = cardsInfo; 
                    gameData["superStarName"] = (turno == 1) ? superstarName1 : superstarName2;
                    gameData["superStarNameOpponent"] = (turno == 1) ? superstarName2 : superstarName1;
                    gameData["playerFortitude"] = (turno == 1) ? player1FortitudeRating : player2FortitudeRating; 
                    gameData["playerFortitudeOpponent"] = (turno == 1) ? player2FortitudeRating : player1FortitudeRating; 
                    gameData["turno"] = turno;
                    gameData["turnoOpponent"] = (turno == 1) ? 2 : 1;
                    gameData["JockeyingEffect"] = JockeyingEffect; // Valor por defecto
                    gameData["JockeyingTurn"] = JockeyingTurn; // Valor por defecto
                    gameData["usedJocking"] = usedJockeying; // Valor por defecto

                    return gameData;
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
                
                void PlayCardForPlayer1()
                {
                    Dictionary<string, object> gameData = CreateGameDataDictionary(1);
                    PlayCardAction(gameData);
                }

                void PlayCardForPlayer2()
                {
                    Dictionary<string, object> gameData = CreateGameDataDictionary(2);
                    PlayCardAction(gameData);
                }


                void PlayCardAction(Dictionary<string, object> gameData)
                {
                    var playableData = PreparePlayableCards(gameData);
                    DisplayAndProcessSelection(playableData, gameData);
                }
                
                Tuple<List<Tuple<int, string>>, List<string>> PreparePlayableCards(Dictionary<string, object> gameData)
                {
                    List<string> playerHand = gameData["playerHand"] as List<string>;
                    List<Card> cardsInfo = gameData["cardsInfo"] as List<Card>;
                    int playerFortitude = (int)gameData["playerFortitude"];

                    var playableCardIndicesAndTypes = new List<Tuple<int, string>>();
                    for (int i = 0; i < playerHand.Count; i++)
                    {
                        var cardInfo = ConvertToCardInfo(playerHand[i], cardsInfo);
                        if (CardIsPlayable(cardInfo, playerFortitude))
                        {
                            foreach (var type in cardInfo.Types)
                            {
                                // Si la carta es híbrida (Action y Reversal), solo agregar la faceta Action
                                if (cardInfo.Types.Contains("Action") && cardInfo.Types.Contains("Reversal"))
                                {
                                    if (type == "Reversal") continue;
                                }
                                playableCardIndicesAndTypes.Add(new Tuple<int, string>(i, type));
                            }
                        }
                    }

                    List<string> cardsToDisplay = FormatPlayableCardsForDisplay(playableCardIndicesAndTypes, playerHand, cardsInfo);
                    return new Tuple<List<Tuple<int, string>>, List<string>>(playableCardIndicesAndTypes, cardsToDisplay);
                }

                
                void DisplayAndProcessSelection(Tuple<List<Tuple<int, string>>, List<string>> playableData, Dictionary<string, object> gameData)
                {
                    int selectedIndex = _view.AskUserToSelectAPlay(playableData.Item2);
                    if (selectedIndex >= 0 && selectedIndex < playableData.Item1.Count)
                    {
                        var selectedCardData = playableData.Item1[selectedIndex];
                        ProcessSelectedCard(selectedCardData.Item1, selectedCardData.Item2, gameData);
                    }
                }
                
                
                void ProcessSelectedCard(int cardIndex, string cardType, Dictionary<string, object> gameData)
                {
                    if (!gameData.ContainsKey("playerHand")) 
                    {
                        Console.WriteLine("Error: La mano del jugador no se encuentra en gameData.");
                        return;
                    }

                    List<string> playerHand = gameData["playerHand"] as List<string>;
                    if (cardIndex < 0 || cardIndex >= playerHand.Count) 
                    {
                        Console.WriteLine("Error: Índice de carta inválido.");
                        return;
                    }

                    string cardName = playerHand[cardIndex];
                    gameData["currentPlayingCard"] = cardName;

                    bool wasReversed = (gameData.ContainsKey("wasReversed")) ? (bool)gameData["wasReversed"] : false;

                    string superStarName = gameData["superStarName"] as string; // Obtener el nombre del superestrella
                    
    
                    ProcessCardAction(cardIndex, cardName, cardType, gameData);
                }




                
            bool ReverseFromDeck(Dictionary<string, object> gameData, Card maneuver, string selectedType)
                {
                    int turno = (int)gameData["turno"];
                    int turnoOponnent = (int)gameData["turnoOpponent"];
                    string superStarName = gameData["superStarNameOpponent"] as string;
                    string superStarNameOpponent = gameData["superStarName"] as string;
                    Console.WriteLine($"Verificando cartas de reversión en la mano de {superStarName}...");
                    
                    
                    List<Tuple<string, int>> validReversalsInHandWithIndices = GetValidReversalsFromHand(gameData, maneuver, selectedType);
                    List<string> validReversalNames = validReversalsInHandWithIndices.Select(t => t.Item1).ToList();
                    // HandleShowHandCardsAction(turnoOponnent);

                    
                    if (validReversalNames.Any())
                    {
                        Console.WriteLine($"El jugador {superStarName} tiene las siguientes cartas de reversión válidas en su mano: {string.Join(", ", validReversalNames)}");
                    }
                    else
                    {
                        Console.WriteLine($"El jugador {superStarName} no tiene cartas de reversión válidas en su mano.");
                    }
                    


                    // Si el jugador tiene cartas de reversión válidas en su mano, ofrézcale la opción de usar una.
                    if (validReversalNames.Any())
                    {
                        
                        List<string> formattedReversals = FormatReversalCardsForDisplay(validReversalNames, gameData["cardsInfo"] as List<Card>);
                        int selectedReversalIndex = _view.AskUserToSelectAReversal(superStarName, formattedReversals);
    
                        if (selectedReversalIndex != -1)
                        {
                            JockeyingEffect=0;
                            JockeyingTurn=0;
                            usedJockeying=false;
                            Tuple<string, int> selectedReversalData = validReversalsInHandWithIndices[selectedReversalIndex];
                            string selectedReversalName = selectedReversalData.Item1;
                            int realIndexInHand = selectedReversalData.Item2;
                            Card reversal = (gameData["cardsInfo"] as List<Card>).FirstOrDefault(c => c.Title == selectedReversalName);
    
                            // Mover la carta de reversión desde la mano del oponente a su área ringside.
                            List<string> playerHandOpponent = gameData["playerHandOpponent"] as List<string>;
                            List<string> playerPile = gameData["playerRingSidePile"] as List<string>;
                            List<string> opponentRingside = gameData["playerRingAreaOpponent"] as List<string>;

                            playerHandOpponent.RemoveAt(realIndexInHand);
                            opponentRingside.Add(selectedReversalName);
                            playerPile.Add(maneuver.Title);

                            List<string> formattedSelectedReversal = FormatReversalCardsForDisplay(new List<string> { selectedReversalName }, gameData["cardsInfo"] as List<Card>);
                            _view.SayThatPlayerReversedTheCard(superStarName, formattedSelectedReversal[0]);

                            // Lógica para "Manager Interferes"
                            if (reversal.Title == "Manager Interferes")
                            {
                                _view.SayThatPlayerDrawCards(superStarName, 1);
                                DrawCard(gameData["playerDeckOpponent"] as List<string>, gameData["playerHandOpponent"] as List<string>);
                            }
                            // Lógica para "Chyna Interferes"
                            else if (reversal.Title == "Chyna Interferes")
                            {
                                _view.SayThatPlayerDrawCards(superStarName, 2);
                                for (int i = 0; i < 2; i++)
                                {
                                    DrawCard(gameData["playerDeckOpponent"] as List<string>, gameData["playerHandOpponent"] as List<string>);
                                }
                            }
                            
                            else if (reversal.Title == "Clean Break" && maneuver.Title == "Jockeying for Position")
                            {
                                // Llamar a PromptPlayerToDiscardCard aquí
                                var currentPlayerHand = gameData["playerHand"] as List<string>;
                                var currentPlayerName = gameData["superStarName"] as string;
                                var remainingDiscards = 4; // Quieres que el jugador descarte 4 cartas

                                for (int i = 0; i < remainingDiscards; i++)
                                {
                                    int cardIndexToDiscard = PromptPlayerToDiscardCard(currentPlayerHand, currentPlayerName, remainingDiscards - i);
                                    if (cardIndexToDiscard >= 0 && cardIndexToDiscard < currentPlayerHand.Count)
                                    {
                                        var discardedCard = currentPlayerHand[cardIndexToDiscard];
                                        playerPile.Add(discardedCard); // Agregamos la carta al Ringside Pile
                                        currentPlayerHand.RemoveAt(cardIndexToDiscard); // Luego la eliminamos de la mano del jugador
                                    }
                                    else
                                    {
                                        Console.WriteLine("Por favor selecciona una carta válida.");
                                        i--; // Decrementa el índice para volver a pedir una carta
                                    }     
                                }
                                _view.SayThatPlayerDrawCards(superStarName, 1);
                                DrawCard(gameData["playerDeckOpponent"] as List<string>, gameData["playerHandOpponent"] as List<string>);
                                JockeyingEffect=0;
                                JockeyingTurn=0;
                                usedJockeying=false;
                            }

                            
                            


                            // Aplicar daño basado en el tipo de reversión

                            if (reversal != null) // Asegurarse de que encontramos la carta de reversión en la lista
                            {
                                if (int.TryParse(reversal.Damage, out int reversalDamage))
                                {
                                    gameData["isReversalDamage"] = true; 
                                    ApplyEffectsBasedOnDamageForReversals(reversalDamage, superStarNameOpponent, gameData);
                                }
                                else if (reversal.Damage.Contains("#") && int.TryParse(maneuver.Damage, out int maneuverDamage))
                                {
                                    gameData["isReversalDamage"] = true;  // Indicamos que el daño proviene de un reversal.
                                    ApplyEffectsBasedOnDamageForNoDamageReversals(maneuverDamage, superStarNameOpponent, gameData);
                                }
                            }

                            if (maneuver.Title != "Jockeying for Position")
                            {
                                HandleEndTurnAction(turno);
                            }
                            return true;
                        }
                    }
                    
                    
                    
            gameData["isReversalDamage"] = false;
            return false; // The maneuver was not reversed
        }



                
            List<Tuple<string, int>> GetValidReversalsFromHand(Dictionary<string, object> gameData, Card maneuver, string selectedType)
            {
                List<string> playerHand = gameData["playerHandOpponent"] as List<string>;
                List<Card> cardsInfo = gameData["cardsInfo"] as List<Card>;
                List<Tuple<string, int>> validReversals = new List<Tuple<string, int>>();

                // Imprimir todas las cartas en la mano del oponente
                Console.WriteLine(maneuver.Title);
                Console.WriteLine(maneuver.Damage);
                Console.WriteLine("Cartas en la mano del oponente:");
                foreach (var cardName in playerHand)
                {
                    Console.WriteLine(cardName);
                }

                for (int i = 0; i < playerHand.Count; i++)
                {
                    string cardName = playerHand[i];
                    Card card = cardsInfo.FirstOrDefault(c => c.Title == cardName);
                    Console.WriteLine($"Verificando carta: {cardName}");
                    if (JockeyingTurn != 0 && JockeyingEffect == 2)
                    {
                        Console.WriteLine($"Fortaleza del jugador (reducida): {(int)gameData["playerFortitudeOpponent"] - 8}");
                        bool isValid = card != null && card.Types.Contains("Reversal") && IsValidReversal(card, maneuver, (int)gameData["playerFortitudeOpponent"] - 8, selectedType, gameData);
                        Console.WriteLine($"¿Es válida? {isValid}");
                        if (isValid)
                        {
                            validReversals.Add(new Tuple<string, int>(cardName, i));
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Fortaleza del jugador: {(int)gameData["playerFortitudeOpponent"]}");
                        bool isValid = card != null && card.Types.Contains("Reversal") && IsValidReversal(card, maneuver, (int)gameData["playerFortitudeOpponent"], selectedType, gameData);
                        Console.WriteLine($"¿Es válida? {isValid}");
                        if (isValid)
                        {
                            validReversals.Add(new Tuple<string, int>(cardName, i));
                        }
                    
                }


                }
                return validReversals;
            }




               bool IsValidReversal(Card reversal, Card cardToReverse, int playerFortitude, string selectedType, Dictionary<string, object> gameData)
{
    if (int.TryParse(reversal.Fortitude, out int reversalFortitude) && reversalFortitude > playerFortitude)
    {
        Console.WriteLine($"{reversal.Title} tiene una fortaleza requerida ({reversalFortitude}) mayor que la fortaleza del jugador ({playerFortitude}). No es una reversión válida.");
        return false;
    }

    if (selectedType == "Maneuver")
    {
        if (cardToReverse.Subtypes.Contains("Strike") && reversal.Subtypes.Contains("ReversalStrike"))
        {
            Console.WriteLine($"{reversal.Title} puede revertir {cardToReverse.Title} ya que ambos son de tipo 'Strike'.");
            return true;
        }
        if (reversal.Subtypes.Contains("ReversalStrikeSpecial") && cardToReverse.Subtypes.Contains("Strike") && int.Parse(cardToReverse.Damage) <= 7)
        {
            Console.WriteLine($"{reversal.Title} can reverse {cardToReverse.Title} since it's a Strike of 7D or less.");
            return true;
        }

        if (reversal.Title == "Escape Move" && cardToReverse.Subtypes.Contains("Grapple"))
        {
            Console.WriteLine($"{reversal.Title} puede revertir {cardToReverse.Title} ya que ambos son de tipo 'Grapple'.");
            return true;
        }
        else if (reversal.Title == "Escape Move")
        {
            Console.WriteLine($"{reversal.Title} no pudo revertir {cardToReverse.Title} ya que {cardToReverse.Title} no es de tipo 'Grapple'.");
        }

        if (cardToReverse.Subtypes.Contains("Grapple") && reversal.Subtypes.Contains("ReversalGrappleSpecial"))
        {
            if (int.TryParse(cardToReverse.Damage, out int damageValue) && damageValue <= 7)
            {
                Console.WriteLine($"{reversal.Title} puede revertir {cardToReverse.Title} ya que ambos son de tipo 'Grapple' y menor a 7D.");
                return true;
            }
        }

        if (reversal.Subtypes.Contains("ReversalSpecial") && reversal.Title != "Jockeying for Position" && reversal.Title != "Clean Break")
        {
            if (int.TryParse(cardToReverse.Damage, out int damageValue) && damageValue <= 7)
            {
                Console.WriteLine($"{reversal.Title} can reverse {cardToReverse.Title} since it's a maneuver of 7D or less.");
                return true;
            }

            // Manager Interferes
            if (reversal.Title == "Manager Interferes")
            {
                Console.WriteLine($"{reversal.Title} puede revertir cualquier maniobra.");
                return true;
            }

            // Chyna Interferes
            if (reversal.Title == "Chyna Interferes" && reversal.Subtypes.Contains("ReversalSpecial") && (reversal.Subtypes.Contains("HHH") || reversal.Subtypes.Contains("Unique")))
            {
                Console.WriteLine($"{reversal.Title} puede revertir cualquier maniobra.");
                return true;
            }
        }

        if (cardToReverse.Subtypes.Contains("Submission") && reversal.Subtypes.Contains("ReversalSubmission"))
        {
            Console.WriteLine($"{reversal.Title} puede revertir {cardToReverse.Title} ya que ambos son de tipo 'Submission'.");
            return true;
        }
    }

    if (reversal.Title == "Jockeying for Position" && cardToReverse.Title == "Jockeying for Position")
    {
        Console.WriteLine($"The card {reversal.Title} can reverse another {cardToReverse.Title}.");
        return true;
    }

    if (reversal.Title == "Clean Break" && cardToReverse.Title == "Jockeying for Position")
    {
        Console.WriteLine($"{reversal.Title} can reverse {cardToReverse.Title}.");
        return true;
    }

    if (selectedType == "Action" && reversal.Subtypes.Contains("ReversalAction"))
    {
        Console.WriteLine($"Comparando tipo seleccionado '{selectedType}' con 'Action' y verificando si {reversal.Title} contiene 'ReversalAction'. Resultado: {selectedType == "Action" && reversal.Subtypes.Contains("ReversalAction")}");
        return true;
    }

    Console.WriteLine($"{reversal.Title} no puede revertir {cardToReverse.Title}. Tipos incompatibles.");
    return false;
}


                
                
                void ApplyStunValue(Dictionary<string, object> gameData, RawDealView.Formatters.IViewableCardInfo cardInfo)
                {
                    string superStarName = gameData["superStarName"] as string;

                    if (int.TryParse(cardInfo.StunValue, out int stunValue) && stunValue > 0)
                    {

                        int cardsToDraw = _view.AskHowManyCardsToDrawBecauseOfStunValue(superStarName, stunValue);
                        if (cardsToDraw > 0 && cardsToDraw <= stunValue)
                        {
                            for (int i = 0; i < cardsToDraw; i++)
                            {
                                if ((gameData["playerDeck"] as List<string>).Count > 0)
                                {
                                    DrawCard(gameData["playerDeck"] as List<string>, gameData["playerHand"] as List<string>);
                                }
                                else
                                {
                                    Console.WriteLine("El mazo del jugador se ha agotado.");
                                    break;
                                }
                            }
                        }
                    }
                }
                
                
              void ProcessCardAction(int cardIndex, string cardName, string selectedType, Dictionary<string, object> gameData)
                {
                    gameData["selectedType"] = selectedType;
                    int currentTurn = (int)gameData["turno"];
                    wasDamageIncreased = false;
                    Console.WriteLine($"CURRRENT TURN {currentTurn}");
                    Console.WriteLine($"CURRRENT TURN {JockeyingTurn}");

                    string superStarName = gameData["superStarName"] as string;

                    var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                    List<Card> cardPlayeds = gameData["cardsInfo"] as List<Card>;
                    (gameData["playerHand"] as List<string>).RemoveAt(cardIndex);

                    var cardPlayed = cardPlayeds.FirstOrDefault(c => c.Title == cardName);

                    if (int.TryParse(cardInfo.StunValue, out int stunValue))
                    {
                        gameData["currentStunValue"] = stunValue;
                    }
                    else
                    {
                        gameData["currentStunValue"] = 0;
                    }

                    if (cardName == "Jockeying for Position")
                    {
                        ProcessJockeyingForPosition(cardPlayed, gameData);
                    }
                    else
                    {
                        ProcessOtherCard(cardPlayed, selectedType, gameData);
                    }
                }

                void ProcessJockeyingForPosition(Card cardPlayed, Dictionary<string, object> gameData)
                {
                    Console.WriteLine("[DEBUG] Procesando 'Jockeying for Position'");

                    var superStarName = gameData["superStarName"] as string;
                    var playInfo = new PlayInfo(
                        cardPlayed.Title,
                        "Action",
                        cardPlayed.Fortitude,
                        cardPlayed.Damage,
                        cardPlayed.StunValue,
                        cardPlayed,
                        "ACTION");

                    _view.SayThatPlayerIsTryingToPlayThisCard(superStarName, RawDealView.Formatters.Formatter.PlayToString(playInfo));

                    bool wasReversed = ReverseFromDeck(gameData, cardPlayed, "Action");

                    if (wasReversed)
                    {
                        Console.WriteLine("[DEBUG] 'Jockeying for Position' fue revertido");

                        // Aquí, verificamos si la carta de reversión también es "Jockeying for Position" antes de aplicar el efecto.
                        var selectedReversalCard = (gameData["playerRingAreaOpponent"] as List<string>).LastOrDefault();
                        if (selectedReversalCard == "Jockeying for Position")
                        {
                            Console.WriteLine("por lo menos entre");
                            ApplyJockeyingForPositionEffectAsReversal(gameData);
                        }

                        HandleEndTurnAction((int)gameData["turno"]);
                    }
                    else
                    {
                        _view.SayThatPlayerSuccessfullyPlayedACard();
                        Console.WriteLine("[DEBUG] 'Jockeying for Position' fue jugado con éxito");
                        ApplyJockeyingForPositionEffectAsAction(gameData);
                    }
                }

                
                
                void ApplyJockeyingForPositionEffectAsAction(Dictionary<string, object> gameData)
                {
                    Console.WriteLine("[DEBUG] Inicio de ApplyJockeyingForPositionEffectAsAction");

                    string superStarName = gameData["superStarName"] as string;

                    RawDealView.Options.SelectedEffect effectChoiceEnum = _view.AskUserToSelectAnEffectForJockeyForPosition(superStarName);
                    int effectChoice = (int)effectChoiceEnum;

                    JockeyingEffect = effectChoice + 1;
                    JockeyingTurn = (int)gameData["turno"];
                    usedJockeying = false;

                    Console.WriteLine($"[DEBUG] JockeyingEffect: {JockeyingEffect}");
                    Console.WriteLine($"[DEBUG] JockeyingTurn: {JockeyingTurn}");
                    Console.WriteLine($"[DEBUG] usedJocking: {usedJockeying}");

                    Console.WriteLine("[DEBUG] Fin de ApplyJockeyingForPositionEffectAsAction");
                }

                void ApplyJockeyingForPositionEffectAsReversal(Dictionary<string, object> gameData)
                {
                    Console.WriteLine("[DEBUG] Inicio de ApplyJockeyingForPositionEffectAsReversal");

                    string superStarName = gameData["superStarNameOpponent"] as string;

                    RawDealView.Options.SelectedEffect effectChoiceEnum = _view.AskUserToSelectAnEffectForJockeyForPosition(superStarName);
                    int effectChoice = (int)effectChoiceEnum;

                    JockeyingEffect = effectChoice + 1;
                    JockeyingTurn = (int)gameData["turnoOpponent"];
                    usedJockeying = false;
                    
                    Console.WriteLine($"[DEBUG] JockeyingEffect: {JockeyingEffect}");
                    Console.WriteLine($"[DEBUG] JockeyingTurn: {JockeyingTurn}");
                    Console.WriteLine($"[DEBUG] usedJocking: {usedJockeying}");
                    

                    Console.WriteLine("[DEBUG] Fin de ApplyJockeyingForPositionEffectAsReversal");
                }

                
      
                void ExtractAndApplyEffects(string cardName, Dictionary<string, object> gameData)
                {
                    // Obtener la información de las cartas desde el gameData
                    List<Card> cardsInfo = gameData["cardsInfo"] as List<Card>;

                    // Obtener el nombre del SuperStar del oponente
                    string superStarNameOpponent = gameData["superStarNameOpponent"] as string;

                    // Imprimir información relevante
                    Console.WriteLine($"[DEBUG] Extracting and applying effects for card: {cardName}");
                    Console.WriteLine($"[DEBUG] Opponent's superstar name: {superStarNameOpponent}");

                    _view.SayThatPlayerSuccessfullyPlayedACard();

                    // Calcular el daño real basado en el nombre de la carta
                    int realDamage = CalculateDamage(cardName, cardsInfo);
                    Console.WriteLine($"[DEBUG] Calculated real damage: {realDamage}");

                    // Ajustar el daño si el oponente es MANKIND y el daño real es mayor a 0
                    int adjustedDamage = (superStarNameOpponent == "MANKIND" && realDamage > 0) ? realDamage - 1 : realDamage;
                    Console.WriteLine($"[DEBUG] Adjusted damage (after considering MANKIND effect if applicable): {adjustedDamage}");

                    // Aplicar efectos basados en el daño ajustado
                    ApplyEffectsBasedOnDamage(adjustedDamage, superStarNameOpponent, gameData);
                }



                
          void DisplayPlayerAction(string superStarName, string cardName, List<Card> cardsInfo, string selectedType)
          {
              var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
              var playInfo = new PlayInfo(
                  cardInfo.Title,
                  selectedType,
                  cardInfo.Fortitude,
                  cardInfo.Damage,
                  cardInfo.StunValue,
                  cardInfo,
                  selectedType.ToUpper()
              );

              // Print the card types
              
              Console.WriteLine("GUATON RE CTMERESKLDJAS");

              _view.SayThatPlayerIsTryingToPlayThisCard(superStarName,
                  RawDealView.Formatters.Formatter.PlayToString(playInfo));
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

                
                
                List<string> FormatPlayableCardsForDisplay(List<Tuple<int, string>> playableCardIndicesAndTypes, List<string> playerHand, List<Card> cardsInfo)
                {
                    var formattedCards = new List<string>();
                    foreach (var tuple in playableCardIndicesAndTypes)
                    {
                        int index = tuple.Item1;
                        string type = tuple.Item2;
                        var cardInfo = ConvertToCardInfo(playerHand[index], cardsInfo);
                        var playInfo = new PlayInfo(
                            cardInfo.Title,
                            type, // Use the type from the tuple
                            cardInfo.Fortitude,
                            cardInfo.Damage,
                            cardInfo.StunValue,
                            cardInfo,
                            type.ToUpper() // Use the type from the tuple
                        );
                        formattedCards.Add(RawDealView.Formatters.Formatter.PlayToString(playInfo));
                    }
                    return formattedCards;
                }
                
                
                List<string> FormatReversalCardsForDisplay(List<string> reversalCards, List<Card> cardsInfo)
                {
                    var formattedCards = new List<string>();
                    foreach (var cardName in reversalCards)
                    {
                        var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                        var playInfo = new PlayInfo(
                            cardInfo.Title,
                            "Reversal", // Tipo de carta
                            cardInfo.Fortitude,
                            cardInfo.Damage,
                            cardInfo.StunValue,
                            cardInfo,
                            "REVERSAL" // Tipo de carta en mayúsculas
                        );
                        formattedCards.Add(RawDealView.Formatters.Formatter.PlayToString(playInfo));
                    }
                    return formattedCards;
                }


                void ApplyCardEffects(string cardName, Dictionary<string, object> gameData)
                {
                    ExtractAndApplyEffects(cardName, gameData);
                }
                
                
                void ApplyEffectsBasedOnDamage(int damage, string superStarNameOpponent, Dictionary<string, object> gameData)
                {
                    if (JockeyingTurn != 0 && JockeyingEffect == 1)
                    {
                        IncreaseFortitudeForSuperstar(damage-4, superStarNameOpponent, gameData);  // Movido fuera de la condición
                    }
                    else
                    {
                        IncreaseFortitudeForSuperstar(damage, superStarNameOpponent, gameData);  // Movido fuera de la condición
                    }
                    
                    if (damage <= 0) return;

                    _view.SayThatSuperstarWillTakeSomeDamage(superStarNameOpponent, damage);
                    OverturnCardsForDamage(damage, gameData);
                }
                
                void ApplyEffectsBasedOnDamageForReversals(int damage, string superStarName, Dictionary<string, object> gameData)
                {
                    string superStarNameOpponent = gameData["superStarNameOpponent"] as string;
                    string superStarNameActual = gameData["superStarName"] as string;

                    // Guarda el daño original
                    int originalDamage = damage;

                    // Ajusta el daño si el oponente es Mankind
                    if (superStarNameActual == "MANKIND" && damage > 0)
                    {
                        damage -= 1;
                    }

                    Console.WriteLine(damage);

                    // Usa el daño original para calcular el aumento de fortaleza
                    IncreaseFortitudeForSuperstarForReversals(originalDamage, superStarNameOpponent, gameData); 

                    if (damage <= 0) return;

                    _view.SayThatSuperstarWillTakeSomeDamage(superStarName, damage);
                    OverturnCardsForDamageForReversals(damage, gameData);
                }


                void ApplyEffectsBasedOnDamageForNoDamageReversals(int damage, string superStarName, Dictionary<string, object> gameData)
                {
                    string superStarNameActual = gameData["superStarName"] as string;
                    string superStarNameOpponent = gameData["superStarNameOpponent"] as string;

                    // Ajusta el daño si el oponente es Mankind
                    if (superStarNameActual == "MANKIND" && damage > 0)
                    {
                        damage -= 1;
                    }
                    if (superStarNameOpponent == "MANKIND" && damage > 0)
                    {
                        damage -= 1;
                    }

                    Console.WriteLine(damage);

                    if (damage <= 0) return;

                    _view.SayThatSuperstarWillTakeSomeDamage(superStarName, damage);
                    OverturnCardsForDamageForReversals(damage, gameData);
                }

                
                void OverturnCardsForDamageForReversals(int damage, Dictionary<string, object> gameData)
                {
                    List<string> playerDeck = gameData["playerDeck"] as List<string>;
                    for (int i = 0; i < damage; i++)
                        ProcessDamageForReversals(i, damage, gameData, playerDeck, gameData["playerRingSidePile"] as List<string>, gameData["cardsInfo"] as List<Card>);
                }

                void IncreaseFortitudeForSuperstar(int damage, string superStarNameOpponent, Dictionary<string, object> gameData)
                {
                    Console.WriteLine($"[DEBUG] Entrando a 'IncreaseFortitudeForSuperstar' con daño: {damage}");

                    int turno = (int)gameData["turno"];
         

                    Console.WriteLine($"[DEBUG] Nombre del SuperStar del oponente: {superStarNameOpponent}");

                    int fortitudeIncrease = (superStarNameOpponent == "MANKIND") ? damage + 1 : damage;
                    Console.WriteLine($"[DEBUG] Aumento de fortaleza calculado: {fortitudeIncrease}");

                    IncreaseFortitude(fortitudeIncrease, turno);
                    Console.WriteLine($"[DEBUG] Fortaleza aumentada para el turno {turno} en {fortitudeIncrease}");
                }
                
                void IncreaseFortitudeForSuperstarForReversals(int damage, string superStarNameOpponent, Dictionary<string, object> gameData)
                {
                    Console.WriteLine($"[DEBUG] Entrando a 'IncreaseFortitudeForSuperstar' con daño: {damage}");
                    string superStarName = gameData["superStarName"] as string;

                    int turno = (int)gameData["turnoOpponent"];

                    Console.WriteLine($"[DEBUG] Nombre del SuperStar del oponente: {superStarNameOpponent}");

                    if (damage > 0)
                    {
                        int fortitudeIncrease = damage;

                        Console.WriteLine($"[DEBUG] Aumento de fortaleza calculado: {fortitudeIncrease}");

                        IncreaseFortitude(fortitudeIncrease, turno);
                    }   
                }


                
                int CalculateDamage(string cardName, List<Card> cardsInfo)
                {
                    var cardInfo = cardsInfo.FirstOrDefault(card => card.Title == cardName);
                    int damageValue = (cardInfo != null && int.TryParse(cardInfo.Damage, out int result)) ? result : 0;
                    return damageValue;
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
                
                void OverturnCardsForDamage(int damage, Dictionary<string, object> gameData)
                {
                    
                    List<string> playerDeckOpponent = gameData["playerDeckOpponent"] as List<string>;
                    for (int i = 0; i < damage; i++)
                        ProcessDamage(i, damage, gameData, playerDeckOpponent, gameData["ringSidePileOpponent"] as List<string>, gameData["cardsInfo"] as List<Card>);
                }

                
                void ProcessDamage(int i, int damage, Dictionary<string, object> gameData, List<string> playerDeckOpponent, List<string> ringSidePileOpponent, List<Card> cardsInfo)
                {
                    if (playerDeckOpponent.Count == 0) EndGame((int)gameData["turno"], gameData["superStarName"] as string);
                    HandleCard(playerDeckOpponent, ringSidePileOpponent, cardsInfo, i, damage, gameData);
                }
                
                void ProcessDamageForReversals(int i, int damage, Dictionary<string, object> gameData, List<string> playerDeckOpponent, List<string> ringSidePileOpponent, List<Card> cardsInfo)
                {
                    if (playerDeckOpponent.Count == 0) EndGame((int)gameData["turnoOpponent"], gameData["superStarNameOpponent"] as string);
                    HandleCard(playerDeckOpponent, ringSidePileOpponent, cardsInfo, i, damage, gameData);
                }

                
              void HandleCard(List<string> playerDeckOpponent, List<string> ringSidePileOpponent, List<Card> cardsInfo, int i, int damage, Dictionary<string, object> gameData)
            {
                string cardName = playerDeckOpponent.Last();
                int turno = (int)gameData["turno"];
                string superStarName = gameData["superStarName"] as string;
                string superStarNameOpponent = gameData["superStarNameOpponent"] as string;

                string currentPlayingCardName = gameData["currentPlayingCard"] as string;
                Card currentPlayingCard = cardsInfo.FirstOrDefault(c => c.Title == currentPlayingCardName);

                playerDeckOpponent.RemoveAt(playerDeckOpponent.Count - 1);
                ringSidePileOpponent.Add(cardName);

                Card topCard = cardsInfo.FirstOrDefault(c => c.Title == cardName);
                if (wasDamageIncreased) // Si el daño fue aumentado debido a "Jockeying for Position"
                {
                    IncreaseDamageOfCard(currentPlayingCardName, cardsInfo, -4);
    
                }
                _view.ShowCardOverturnByTakingDamage(RawDealView.Formatters.Formatter.CardToString(ConvertToCardInfo(cardName, cardsInfo)), i + 1, damage);
                if (wasDamageIncreased) // Si el daño fue aumentado debido a "Jockeying for Position"
                {
                    IncreaseDamageOfCard(currentPlayingCardName, cardsInfo, 4);
                }

                int originalFortitude = (int)gameData["playerFortitudeOpponent"];
                if ((int)gameData["JockeyingTurn"] != 0 && (int)gameData["JockeyingEffect"] == 2)
                {
                    gameData["playerFortitudeOpponent"] = originalFortitude - 8;
                }

                // Si el daño proviene de un reversal, no permitas que el jugador activo revierta desde su mazo.
                if (gameData.ContainsKey("isReversalDamage") && (bool)gameData["isReversalDamage"])
                {
                    return;
                }
                if (gameData.ContainsKey("isReversalDamage") && (bool)gameData["isReversalDamage"])
                {
                    Console.WriteLine($"[DEBUG] {superStarName} intentó revertir durante un daño de reversal, pero no se le permite.");
                    return;
                }

                if (topCard != null && topCard.Types.Contains("Reversal") && IsValidReversal(topCard, currentPlayingCard, (int)gameData["playerFortitudeOpponent"], (string)gameData["selectedType"], gameData))
                {
                    Console.WriteLine($"[HandleCard] {topCard.Title} es un reversal válido contra {currentPlayingCard.Title}");
                    if (wasDamageIncreased) // Si el daño fue aumentado debido a "Jockeying for Position"
                    {
                        IncreaseDamageOfCard(currentPlayingCardName, cardsInfo, -4);
                        wasDamageIncreased = false;
                    }

                    _view.SayThatCardWasReversedByDeck(superStarNameOpponent);
                    JockeyingEffect = 0;
                    JockeyingTurn = 0;
                    usedJockeying = false;
                    
                    

                    if (i + 1 == damage) // Si la carta volteada es la última en el daño
                    {
                        HandleEndTurnAction(turno);
                        return;
                    }

                    if (gameData.ContainsKey("currentStunValue"))
                    {
                        Console.WriteLine("[HandleCard] gameData contiene la clave 'currentStunValue'");
                        int currentStunValue = (int)gameData["currentStunValue"];
                        Console.WriteLine($"[HandleCard] currentStunValue: {currentStunValue}");

                        if (currentStunValue > 0)
                        {
                            int cardsToDraw = _view.AskHowManyCardsToDrawBecauseOfStunValue(superStarName, currentStunValue);
                            _view.SayThatPlayerDrawCards(superStarName, cardsToDraw);
                            for (int drawIndex = 0; drawIndex < cardsToDraw; drawIndex++)
                            {
                                DrawCard(gameData["playerDeck"] as List<string>, gameData["playerHand"] as List<string>);
                            }
                            gameData["currentStunValue"] = 0;  // Reset "Stun Value" después de aplicarlo.
                        }
                        else
                        {
                            Console.WriteLine("[HandleCard] currentStunValue es 0 o menor.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[HandleCard] gameData no contiene la clave 'currentStunValue'");
                    }
                    HandleEndTurnAction(turno);
                }

                // Asegúrate de restaurar el valor original de fortaleza antes de salir de la función
                gameData["playerFortitudeOpponent"] = originalFortitude;
                JockeyingEffect=0;
                JockeyingTurn=0;
                usedJockeying=false;
            }


                

                void DisplayCards(List<string> cardNames)
                {
                    List<RawDealView.Formatters.IViewableCardInfo> viewableCardsInfo = 
                        cardNames.Select(cardName => ConvertToCardInfo(cardName, cardsInfo)).ToList();

                    List<string> cardsToDisplay = viewableCardsInfo
                        .Select(cardInfo => RawDealView.Formatters.Formatter.CardToString(cardInfo))
                        .ToList();

                    _view.ShowCards(cardsToDisplay);
                }

                void ShowPlayerHandCards(List<string> playerHand)
                {
                    DisplayCards(playerHand);
                }

                void ShowPlayerRingArea(List<string> ringArea)
                {
                    DisplayCards(ringArea);
                }

                void ShowPlayerRingsidePile(List<string> ringsidePile)
                {
                    DisplayCards(ringsidePile);
                }


                void IncreaseDamageOfCard(string cardName, List<Card> cardsInfo, int additionalDamage)
                    {
                        var card = cardsInfo.FirstOrDefault(c => c.Title == cardName);
                        if (card != null && int.TryParse(card.Damage, out int currentDamage))
                        {
                            card.Damage = (currentDamage + additionalDamage).ToString();
                        }
                    }

                
                    void IncreaseReversalFortitudeRequirement(Dictionary<string, object> gameData, int additionalFortitude)
                    {
                        // Vamos a añadir un modificador al gameData que indica cuánto fortitude adicional se requiere para jugar un reversal
                        gameData["ReversalFortitudeModifier"] = additionalFortitude;
                    }


                
                

                
                RawDealView.Formatters.IViewableCardInfo ConvertToCardInfo(string cardName, List<Card> cardsInfoList)
                {
                    var cardData = cardsInfoList.FirstOrDefault(c => c.Title == cardName);
                    if (cardData != null)
                    {
                        return new CardInfo(cardData.Title, cardData.Fortitude, cardData.Damage, cardData.StunValue,
                            cardData.Types, cardData.Subtypes, cardData.CardEffect);
                    }
                    return null; 
                }
                
                

                void EndGame(int turno, string superStarName)
                {
                    CongratulateWinner((turno == 1) ? 2 : 1);
                    throw new GameEndException(superStarName);
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
                    {
                        return File.ReadAllLines(filePath).ToList();
                    }
                }


                List<Card> LoadCardsInfo(string filePath)
                {
                    List<Card> cardsInfo = new List<Card>();
                    string json = File.ReadAllText(filePath);
                    cardsInfo = JsonSerializer.Deserialize<List<Card>>(json);
                
                    return cardsInfo;
                }


                List<Superstar> LoadSuperstarInfo(string filePath)
                {
                    try
                    {
                        string json = File.ReadAllText(filePath);
                        return JsonSerializer.Deserialize<List<Superstar>>(json) ?? new List<Superstar>();
                    }
                    catch (Exception ex)
                    {
                        return new List<Superstar>();
                    }
                }
                

                bool IsEveryCardValid(List<string> deck, List<Card> cardsInfo)
                {
                    foreach (string cardTitle in deck)
                    {
                        Card card = cardsInfo.FirstOrDefault(c => c.Title == cardTitle);

                        if (card == null)
                        {
                            return false;
                        }
                    }
                    return true;
                }


                bool HasUniqueTitles(List<string> deck, List<Card> cardsInfo)
                {
                    HashSet<string> uniqueCardTitles = new HashSet<string>();

                    foreach (string cardTitle in deck)
                    {
                        Card card = cardsInfo.FirstOrDefault(c => c.Title == cardTitle);

                        if (card.Subtypes.Contains("Unique") && !card.Subtypes.Contains("SetUp"))
                        {
                            if (uniqueCardTitles.Contains(card.Title))
                            {
                                return false;
                            }
                            uniqueCardTitles.Add(card.Title);
                        }
                    }

                    return true;
                }


                bool AreCardTitlesUnique(List<string> deck, List<Card> cardsInfo)
                {
                    return IsEveryCardValid(deck, cardsInfo) && HasUniqueTitles(deck, cardsInfo);
                }

                
                bool IsCardPresentInInfo(List<string> deck, List<Card> cardsInfo)
                {
                    foreach (string cardTitle in deck)
                    {
                        Card card = cardsInfo.FirstOrDefault(c => c.Title == cardTitle);
                        if (card == null)
                        {
                            return false;
                        }
                    }
                    return true;
                }
                
                
                bool HasSubtype(string cardTitle, List<Card> cardsInfo, string subtype)
                {
                    Card card = cardsInfo.FirstOrDefault(c => c.Title == cardTitle);
                    return card?.Subtypes.Contains(subtype) ?? false;
                }

                
                bool BothHeelAndFacePresent(bool hasHeelCard, bool hasFaceCard)
                {
                    return hasHeelCard && hasFaceCard;
                }

                
                bool CheckSubtypes(List<string> deck, List<Card> cardsInfo)
                {
                    bool hasSetupCard = deck.Any(cardTitle => HasSubtype(cardTitle, cardsInfo, "SetUp"));
                    bool hasHeelCard = deck.Any(cardTitle => HasSubtype(cardTitle, cardsInfo, "Heel"));
                    bool hasFaceCard = deck.Any(cardTitle => HasSubtype(cardTitle, cardsInfo, "Face"));

                    return !BothHeelAndFacePresent(hasHeelCard, hasFaceCard);
                }
                
                
                bool AreSubtypesValid(List<string> deck, List<Card> cardsInfo)
                {
                    return IsCardPresentInInfo(deck, cardsInfo) && CheckSubtypes(deck, cardsInfo);
                }

                
                bool IsValidCardTitlesAndSubtypes(List<string> deck, List<Card> cardsInfo)
                {
                    return AreCardTitlesUnique(deck, cardsInfo) && AreSubtypesValid(deck, cardsInfo);
                }
                

                bool IsCardInInfo(string cardTitle, List<Card> cardsInfo)
                {
                    return cardsInfo.Any(c => c.Title == cardTitle);
                }

                
                void UpdateCardCounts(string cardTitle, Dictionary<string, int> cardCounts)
                {
                    if (!cardCounts.ContainsKey(cardTitle))
                    {
                        cardCounts[cardTitle] = 1;
                    }
                    else
                    {
                        cardCounts[cardTitle]++;
                    }
                }

                
                bool ExceedsMaxCardLimit(Dictionary<string, int> cardCounts, List<Card> cardsInfo)
                {
                    foreach (var pair in cardCounts)
                    {
                        Card currentCard = cardsInfo.FirstOrDefault(c => c.Title == pair.Key);
                        if (pair.Value > 3 && (currentCard == null || !currentCard.Subtypes.Contains("SetUp")))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                
                bool HasValidDeckSize(List<string> deck)
                {
                    return deck.Count == 60;
                }

                
                bool IsValidCardCount(List<string> deck, List<Card> cardsInfo)
                {
                    Dictionary<string, int> cardCounts = new Dictionary<string, int>();

                    foreach (string cardTitle in deck)
                    {
                        if (!IsCardInInfo(cardTitle, cardsInfo))
                        {
                            return false;
                        }
                        UpdateCardCounts(cardTitle, cardCounts);
                    }

                    return !ExceedsMaxCardLimit(cardCounts, cardsInfo) && HasValidDeckSize(deck);
                }

                
            bool IsValidDeckStructure(List<string> deck, List<Card> cardsInfo)
            {
                return IsValidCardTitlesAndSubtypes(deck, cardsInfo) && IsValidCardCount(deck, cardsInfo);
            }

                
            Superstar GetSuperstarByName(List<Superstar> superstarInfo, string superstarName)
            {
                return superstarInfo.FirstOrDefault(s => s.Name == superstarName);
            }

            
            bool IsInvalidSubtypeForSuperstar(Card card, Superstar superstar, List<Superstar> superstarInfo)
            {
                return card.Subtypes.Any(subtype => superstarInfo.Any(s => s.Logo == subtype) && subtype != superstar.Logo);
            }

            
            bool AreAllCardsValidForSuperstar(List<string> deck, List<Card> cardsInfo, Superstar superstar, List<Superstar> superstarInfo)
            {
                foreach (string cardTitle in deck)
                {
                    Card card = cardsInfo.FirstOrDefault(c => c.Title == cardTitle);
                    if (IsInvalidSubtypeForSuperstar(card, superstar, superstarInfo))
                    {
                        return false;
                    }
                }
                return true;
            }

            
            bool IsValidDeckForSuperstar(List<string> deck, List<Card> cardsInfo, List<Superstar> superstarInfo, string superstarName)
            {
                Superstar superstar = GetSuperstarByName(superstarInfo, superstarName);
                if (superstar == null)
                {
                    return false;
                }

                return AreAllCardsValidForSuperstar(deck, cardsInfo, superstar, superstarInfo);
            }
                
                
            bool IsDeckCompletelyValid(List<string> deck, List<Card> cardsInfo, List<Superstar> superstarInfo, string superstarName)
            {
                return IsValidDeckStructure(deck, cardsInfo) && IsValidDeckForSuperstar(deck, cardsInfo, superstarInfo, superstarName);
            }
                
                
            void ApplyKaneAbility(int turn)
            {
                if (IsTurnPlayerOne(turn))
                {
                    UseAbility("KANE", superstar1.SuperstarAbility, superstarName2, player2Deck, player2RingsidePile);
                }
                else
                {
                    UseAbility("KANE", superstar2.SuperstarAbility, superstarName1, player1Deck, player1RingsidePile);
                }
            }

            
            bool IsTurnPlayerOne(int turn)
            {
                return turn == 1;
            }

            
            void UseAbility(string player, string ability, string opponentName, List<string> opponentDeck, List<string> opponentRingside)
            {
                AnnounceAbilityUsage(player, ability);
                AnnounceDamage(opponentName);
                ApplyDamageToDeck(opponentDeck, opponentRingside);
            }

            
            void AnnounceAbilityUsage(string player, string ability)
            {
                _view.SayThatPlayerIsGoingToUseHisAbility(player, ability);
            }

            
            void AnnounceDamage(string opponentName)
            {
                _view.SayThatSuperstarWillTakeSomeDamage(opponentName, 1);
            }

            
            void ApplyDamageToDeck(List<string> opponentDeck, List<string> opponentRingside)
            {
                if (!opponentDeck.Any()) return;
                var overturnedCardName = OverturnCard(opponentDeck);
                opponentRingside.Add(overturnedCardName);
                ShowOverturnedCard(overturnedCardName);
            }

                
            string OverturnCard(List<string> deck)
            {
                string cardName = deck.Last();
                deck.RemoveAt(deck.Count - 1);
                return cardName;
            }

            
            void ShowOverturnedCard(string cardName)
            {
                var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                string cardInfoString = RawDealView.Formatters.Formatter.CardToString(cardInfo);
                _view.ShowCardOverturnByTakingDamage(cardInfoString, 1, 1);
            }
                

            void UseTheRockAbility(int turn)
            {
                if (!IsCurrentPlayerTheRock(turn)) return;
                if (GetCurrentRingSide(turn).Count == 0) return;
                if (!_view.DoesPlayerWantToUseHisAbility("THE ROCK")) return;

                ExecuteRockAbility(turn);
            }

            bool IsCurrentPlayerTheRock(int turn)
            {
                string currentPlayer = GetPlayerName(turn);
                return currentPlayer == "THE ROCK";
            }

            string GetPlayerName(int turn) => (turn == 1) ? superstar1.Name : superstar2.Name;

            List<string> GetCurrentRingSide(int turn) => (turn == 1) ? player1RingsidePile : player2RingsidePile;

            void ExecuteRockAbility(int turn)
            {
                List<string> formattedRingSide = FormatRingSide(GetCurrentRingSide(turn));
                UseAbilityAndRecoverCard(turn, formattedRingSide);
            }

            List<string> FormatRingSide(List<string> ringSide)
            {
                return ringSide.Select(cardName => RawDealView.Formatters.Formatter.CardToString(ConvertToCardInfo(cardName, cardsInfo))).ToList();
            }

            void UseAbilityAndRecoverCard(int turn, List<string> formattedRingSide)
            {
                string currentPlayer = GetPlayerName(turn);
                string superstarAbility = GetSuperstarAbility(turn);
                _view.SayThatPlayerIsGoingToUseHisAbility("THE ROCK", superstarAbility);
                int cardId = _view.AskPlayerToSelectCardsToRecover(currentPlayer, 1, formattedRingSide);
                MoveCardFromRingsideToArsenal(turn, cardId);
            }

            string GetSuperstarAbility(int turn) => (turn == 1) ? superstar1.SuperstarAbility : superstar2.SuperstarAbility;

            void MoveCardFromRingsideToArsenal(int turn, int cardId)
            {
                List<string> currentArsenal = (turn == 1) ? player1Deck : player2Deck;
                List<string> currentRingSide = GetCurrentRingSide(turn);
                currentArsenal.Insert(0, currentRingSide[cardId]);
                currentRingSide.RemoveAt(cardId);
            }

                
            void UseUndertakerAbility(int turn)
            {
                string currentPlayer, superstarAbility;
                List<string> currentPlayerHand, currentPlayerRingside;

                SetupPlayersForTurn(turn, out currentPlayer, out superstarAbility, out currentPlayerHand, out currentPlayerRingside);

                if (IsAbilityUseAllowed(currentPlayer, currentPlayerHand))
                {
                    AnnounceAbilityUsage(currentPlayer, superstarAbility);
                    PerformAbilityActions(currentPlayer, currentPlayerHand, currentPlayerRingside);
                }
            }

            void SetupPlayersForTurn(int turn, out string currentPlayer, out string superstarAbility, out List<string> currentPlayerHand, out List<string> currentPlayerRingside)
            {
                string[] superstarNames = {superstarName1, superstarName2};
                List<string>[] playerHands = {player1Hand, player2Hand};
                List<string>[] playerRingsides = {player1RingsidePile, player2RingsidePile};

                SetupPlayers(turn, superstarNames, playerHands, playerRingsides, out currentPlayer, out superstarAbility, out currentPlayerHand, out currentPlayerRingside);
            }

            bool IsAbilityUseAllowed(string currentPlayer, List<string> currentPlayerHand)
            {
                return currentPlayer == "THE UNDERTAKER" && currentPlayerHand.Count >= 2;
            }
                
            void PerformAbilityActions(string currentPlayer, List<string> currentPlayerHand, List<string> currentPlayerRingside)
            {
                DiscardCards(currentPlayer, currentPlayerHand, currentPlayerRingside);
                RecoverCard(currentPlayer, currentPlayerRingside, currentPlayerHand);
            }


            void SetupPlayers(int turn, string[] superstarNames, List<string>[] playerHands, List<string>[] playerRingsides, out string currentPlayer, out string superstarAbility, out List<string> currentPlayerHand, out List<string> currentPlayerRingside)
            {
                string[] superstarAbilities = {superstar1.SuperstarAbility, superstar2.SuperstarAbility};

                currentPlayer = superstarNames[turn - 1];
                superstarAbility = superstarAbilities[turn - 1];
                currentPlayerHand = playerHands[turn - 1];
                currentPlayerRingside = playerRingsides[turn - 1];
            }

            void DiscardCards(string currentPlayer, List<string> currentPlayerHand, List<string> currentPlayerRingside)
            {
                for (int i = 0; i < 2; i++)
                {
                    int cardIdToDiscard = PromptPlayerToDiscardCard(currentPlayerHand, "THE UNDERTAKER", 2 - i);
                    MoveCardBetweenLists(currentPlayerHand, currentPlayerRingside, cardIdToDiscard);
                }
            }

            void RecoverCard(string currentPlayer, List<string> currentPlayerRingside, List<string> currentPlayerHand)
            {
                int cardIdToRecover = PromptPlayerToRecoverCard(currentPlayerRingside, "THE UNDERTAKER");
                MoveCardBetweenLists(currentPlayerRingside, currentPlayerHand, cardIdToRecover);
            }


            int PromptPlayerToDiscardCard(List<string> hand, string player, int remaining)
            {
                List<string> formattedHand = hand.Select(cardName =>
                {
                    var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                    return RawDealView.Formatters.Formatter.CardToString(cardInfo);
                }).ToList();

                return _view.AskPlayerToSelectACardToDiscard(formattedHand, player, player, remaining);
            }

            int PromptPlayerToRecoverCard(List<string> ringside, string player)
            {
                List<string> formattedRingSide = ringside.Select(cardName =>
                {
                    var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                    return RawDealView.Formatters.Formatter.CardToString(cardInfo);
                }).ToList();

                return _view.AskPlayerToSelectCardsToPutInHisHand(player, 1, formattedRingSide);
            }

            void MoveCardBetweenLists(List<string> source, List<string> destination, int cardId)
            {
                string card = source[cardId];
                source.RemoveAt(cardId);
                destination.Add(card);
            }
            
            
            void InitPlayers(int turn, out PlayerState currentPlayer, out PlayerState opponentPlayer)
            {
                currentPlayer = (turn == 1) 
                    ? new PlayerState { Name = superstarName1, SuperstarAbility = superstar1.SuperstarAbility, Hand = player1Hand, Ringside = player1RingsidePile }
                    : new PlayerState { Name = superstarName2, SuperstarAbility = superstar2.SuperstarAbility, Hand = player2Hand, Ringside = player2RingsidePile };

                opponentPlayer = (turn == 1) 
                    ? new PlayerState { Name = superstarName2, SuperstarAbility = superstar2.SuperstarAbility, Hand = player2Hand, Ringside = player2RingsidePile }
                    : new PlayerState { Name = superstarName1, SuperstarAbility = superstar1.SuperstarAbility, Hand = player1Hand, Ringside = player1RingsidePile };
            }

            
            void UseJerichoAbility(int turn)
            {
                InitPlayers(turn, out PlayerState currentPlayer, out PlayerState opponentPlayer);

                if (currentPlayer.Name == "CHRIS JERICHO" && currentPlayer.Hand.Count >= 1)
                {
                    AnnounceAbilityUsage(currentPlayer.Name, currentPlayer.SuperstarAbility);
                    PlayerDiscardsCard(currentPlayer.Name, currentPlayer.Hand, currentPlayer.Ringside, 1);
                    PlayerDiscardsCard(opponentPlayer.Name, opponentPlayer.Hand, opponentPlayer.Ringside, 1);
                }
            }


            void PlayerDiscardsCard(string player, List<string> hand, List<string> ringside, int number)
            {
                List<string> formattedHand = FormatHand(hand);
                int cardIdToDiscard = _view.AskPlayerToSelectACardToDiscard(formattedHand, player, player, number);
                MoveCardBetweenLists(hand, ringside, cardIdToDiscard);
            }

            List<string> FormatHand(List<string> hand)
            {
                return hand.Select(cardName =>
                {
                    var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                    return RawDealView.Formatters.Formatter.CardToString(cardInfo);
                }).ToList();
            }

            void UseStoneColdAbility(int turn)
            {
                InitStoneCold(turn, out PlayerState currentPlayer, out List<string> currentPlayerDeck);
    
                if (IsAbilityUsable(currentPlayer.Name, currentPlayerDeck))
                {
                    AnnounceAbilityUsage(currentPlayer.Name, currentPlayer.SuperstarAbility);
                    DrawAndAnnounce(currentPlayerDeck, currentPlayer.Hand, currentPlayer.Name);
                    ReturnCardToArsenal(currentPlayer, currentPlayerDeck);
                }
            }

            void InitStoneCold(int turn, out PlayerState currentPlayer, out List<string> currentPlayerDeck)
            {
                InitPlayers(turn, out currentPlayer, out PlayerState opponentPlayer);
                currentPlayerDeck = (turn == 1) ? player1Deck : player2Deck;
            }

            bool IsAbilityUsable(string playerName, List<string> playerDeck)
            {
                return playerName == "STONE COLD STEVE AUSTIN" && playerDeck.Count > 0 && !abilityUsedThisTurn;
            }
                

            void DrawAndAnnounce(List<string> playerDeck, List<string> playerHand, string playerName)
            {
                DrawCard(playerDeck, playerHand);
                _view.SayThatPlayerDrawCards(playerName, 1);
            }

            void ReturnCardToArsenal(PlayerState currentPlayer, List<string> currentPlayerDeck)
            {
                List<string> formattedHand = FormatHand(currentPlayer.Hand);
                int cardIdToReturn = _view.AskPlayerToReturnOneCardFromHisHandToHisArsenal(currentPlayer.Name, formattedHand);
                ReturnSelectedCard(currentPlayer.Hand, currentPlayerDeck, cardIdToReturn);
            }
                

            void ReturnSelectedCard(List<string> hand, List<string> deck, int cardId)
            {
                string returnedCard = hand[cardId];
                hand.RemoveAt(cardId);
                deck.Insert(0, returnedCard); 
                abilityUsedThisTurn = true;
            }
            
            }

                
            catch (GameEndException exception)
            {
                Console.WriteLine("");
            }
        }
    }
}




        
        
