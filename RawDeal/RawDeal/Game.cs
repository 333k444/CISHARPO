
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
        
        
        public Game(View view, string deckFolder)
        {
            _view = view;
            _deckFolder = deckFolder;
        }

        public void Play()
        {
            try
            {
                
                string player1DeckPath = _view.AskUserToSelectDeck(_deckFolder);
                List<string> player1Deck = LoadDeckFromFile(player1DeckPath);
                string superstarName1 = player1Deck[0].Replace(" (Superstar Card)", "");
                player1Deck.RemoveAt(0);

                string cardsPath = Path.Combine("data", "cards.json");
                List<Card> cardsInfo = LoadCardsInfo(cardsPath);

                string superstarPath = Path.Combine("data", "superstar.json");
                List<Superstar> superstarInfo = LoadSuperstarInfo(superstarPath);

                if (!IsDeckCompletelyValid(player1Deck, cardsInfo, superstarInfo, superstarName1))
                {
                    _view.SayThatDeckIsInvalid();
                    return;
                }
                
                string player2DeckPath = _view.AskUserToSelectDeck(_deckFolder);
                List<string> player2Deck = LoadDeckFromFile(player2DeckPath);
                string superstarName2 = player2Deck[0].Replace(" (Superstar Card)", "");
                player2Deck.RemoveAt(0);

                if (!IsDeckCompletelyValid(player2Deck, cardsInfo, superstarInfo, superstarName2))
                {
                    _view.SayThatDeckIsInvalid();
                    return;
                }

                PlayerInfo p1 = null;
                PlayerInfo p2 = null;
                int sval1 = 0;
                int sval2 = 0;
                int handSize1 = 0;
                int arsenalSize1 = 0;
                int handSize2 = 0;
                int arsenalSize2 = 0;
                Superstar superstar1 = superstarInfo.FirstOrDefault(s => s.Name == superstarName1);
                if (superstar1 != null)
                {
                    handSize1 = superstar1.HandSize;
                    player1Hand = player1Deck.GetRange(player1Deck.Count - handSize1, handSize1);
                    player1Hand.Reverse();
                    player1Deck.RemoveRange(player1Deck.Count - handSize1, handSize1);
                    sval1 = superstar1.SuperstarValue;
                    player1FortitudeRating = 0;

                }

                Superstar superstar2 = superstarInfo.FirstOrDefault(s => s.Name == superstarName2);
                if (superstar2 != null)
                {
                    handSize2 = superstar2.HandSize;
                    player2Hand = player2Deck.GetRange(player2Deck.Count - handSize2, handSize2);
                    player2Hand.Reverse();
                    player2Deck.RemoveRange(player2Deck.Count - handSize2, handSize2);
                    sval2 = superstar2.SuperstarValue;
                    player2FortitudeRating = 0;

                }

                if (sval1 > sval2)
                {
                    startingPlayer = 1;
                    _view.SayThatATurnBegins(superstarName1);
                    HandlePlayerActions(1);

                }
                else if (sval2 > sval1)
                {

                    startingPlayer = 2;
                    _view.SayThatATurnBegins(superstarName2);
                    HandlePlayerActions(2);


                }
                else
                {
                    startingPlayer = 1;
                    _view.SayThatATurnBegins(superstarName1);
                    HandlePlayerActions(1);

                }
                

                void HandlePlayerActions(int turno)

                {
                    
                    string currentPlayer = (turno == 1) ? superstarName1 : superstarName2;
                    

                    PlayerInfo player1 = new PlayerInfo(superstarName1, player1FortitudeRating,
                        player1Hand.Count,
                        player1Deck.Count);
                    PlayerInfo player2 = new PlayerInfo(superstarName2, player2FortitudeRating,
                        player2Hand.Count,
                        player2Deck.Count);


                    if (turno == 1)

                    {
                        
                        UseTheRockAbility(turno);
                        
                        DrawCard(player1Deck, player1Hand, turno);
                        UpdatePlayerInfo(out player1, out player2);
                        
                        
                        if (superstarName1.ToUpper() == "KANE")
                        {
                            ApplyKaneAbility(1);
                            UpdatePlayerInfo(out player1, out player2);
                            turno = (turno == 1) ? 2 : 1;
                                  
                        }
                        
                        UpdatePlayerInfo(out player1, out player2);
                    }

                    else
                    {
                        
                        UseTheRockAbility(turno);
                        
                        DrawCard(player2Deck, player2Hand, turno);
                        UpdatePlayerInfo(out player2, out player2);
                        
                        if (superstarName2.ToUpper() == "KANE")
                        {
                            ApplyKaneAbility(1);
                            UpdatePlayerInfo(out player1, out player2);
                            turno = (turno == 1) ? 2 : 1;
                                  
                        }
                        
                        UpdatePlayerInfo(out player1, out player2);
                    }

                    if (startingPlayer == 1)
                    {
                        _view.ShowGameInfo(player1, player2);
                    }
                    else
                    {
                        _view.ShowGameInfo(player2, player1);
                    }


                    NextPlay action;
                    
                    
                    if (currentPlayer == "THE UNDERTAKER")

                    {
                        action = _view.AskUserWhatToDoWhenUsingHisAbilityIsPossible();

                    }

                    else
                    {
                        action = _view.AskUserWhatToDoWhenHeCannotUseHisAbility();

                    }
                    

                    while (action != NextPlay.GiveUp)

                    {
                        
                        switch (action)
                        {
                            
                            
                            case NextPlay.UseAbility:
                                if (currentPlayer == "THE UNDERTAKER")
                                    
                                    {
                                        UseUndertakerAbility(turno);
                                    }

                                break;
                                
                                
                            case NextPlay.ShowCards:

                                CardSet cardSetChoice = _view.AskUserWhatSetOfCardsHeWantsToSee();
                                switch (cardSetChoice)
                                {
                                    case CardSet.Hand:

                                        if (turno == 1)

                                        {
                                            ShowPlayerHandCards(player1Hand); // Mostrar la mano del jugador actual
                                        }

                                        else
                                        {
                                            ShowPlayerHandCards(player2Hand); // Mostrar la mano del jugador actual
                                        }

                                        break;

                                    case CardSet.RingArea:

                                        if (turno == 1)

                                        {
                                            ShowPlayerRingArea(player1RingArea);
                                        }

                                        else
                                        {
                                            ShowPlayerRingArea(player2RingArea);
                                        }

                                        break;

                                    case CardSet.RingsidePile:

                                        if (turno == 1)

                                        {
                                            ShowPlayerRingArea(player1RingsidePile);
                                        }

                                        else
                                        {
                                            ShowPlayerRingArea(player2RingsidePile);
                                        }

                                        break;

                                    case CardSet.OpponentsRingArea:
                                        if (turno == 1)

                                        {
                                            ShowPlayerRingArea(player2RingArea);
                                        }

                                        else
                                        {
                                            ShowPlayerRingArea(player1RingArea);
                                        }

                                        break;

                                    case CardSet.OpponentsRingsidePile:
                                        if (turno == 1)

                                        {
                                            ShowPlayerRingsidePile(player2RingsidePile);
                                        }

                                        else
                                        {
                                            ShowPlayerRingsidePile(player1RingsidePile);
                                        }

                                        break;
                                }

                                break;

                            case NextPlay.PlayCard:

                                if (turno == 1)

                                {

                                    PlayCardAction(player1Hand, player2Hand, player1Deck,
                                        player2Deck,
                                        player1RingsidePile, player2RingsidePile,
                                        cardsInfo, superstarName1, superstarName2,
                                        player1FortitudeRating, turno
                                    );
                                    UpdatePlayerInfo(out player1, out player2);

                                }

                                else
                                {
                                    PlayCardAction(player2Hand, player1Hand, player2Deck,
                                        player1Deck, player2RingsidePile,
                                        player1RingsidePile,
                                        cardsInfo, superstarName2, superstarName1,
                                        player2FortitudeRating, turno
                                    );
                                    UpdatePlayerInfo(out player1, out player2);
                                }

                                break;
                            
                                
                                
                            case NextPlay.EndTurn:
                                
                                if (turno == 1 && player2Deck.Count == 0)
                                {
                                    _view.CongratulateWinner(superstarName1);
                                    throw new GameEndException(superstarName1);

                                }

                                if (turno == 2 && player1Deck.Count == 0)
                                {
                                    _view.CongratulateWinner(superstarName2);
                                    throw new GameEndException(superstarName2);

                                }

                                if (turno == 1)
                                {
                                    
                                    turno = (turno == 1) ? 2 : 1;
                                    
                                    _view.SayThatATurnBegins(superstarName2);
                                    
                                    UseTheRockAbility(turno);
                                    UseUndertakerAbility(turno);
                                    
                                    if (superstarName2.ToUpper() == "KANE")
                                    {
                                        ApplyKaneAbility(2);
                                        UpdatePlayerInfo(out player1, out player2);
                                    }
                                    
                                    DrawCard(player2Deck, player2Hand, turno);
                                    UpdatePlayerInfo(out player1, out player2);
                           
                                }

                                else
                                {
                                   
                                    
                                    turno = (turno == 1) ? 2 : 1;
                                    
                                    
                                    UpdatePlayerInfo(out player1, out player2);
                                    _view.SayThatATurnBegins(superstarName1);

                                    UseTheRockAbility(turno);
                                    UseUndertakerAbility(turno);
                                    if (superstarName1.ToUpper() == "KANE")
                                    {
                                        ApplyKaneAbility(1);
                                        UpdatePlayerInfo(out player1, out player2);
                                  
                                  
                                    }
                                    
                                    DrawCard(player1Deck, player1Hand, turno);
                                    UpdatePlayerInfo(out player1, out player2);
                                }


                                break;

                        }

                        if (turno == 1)
                        {
                            _view.ShowGameInfo(player1, player2);
                        }
                        else
                        {
                            _view.ShowGameInfo(player2, player1);
                        }

                        if (currentPlayer == "THE UNDERTAKER")
                        {
                            action = _view.AskUserWhatToDoWhenUsingHisAbilityIsPossible();
                        }
                        else
                        {
                            action = _view.AskUserWhatToDoWhenHeCannotUseHisAbility();
                        }
                        
                        
                    }
                    
                    
                    
                    if (turno == 1)
                    {
                        _view.CongratulateWinner(superstarName2);
                    }
                    else
                    {
                        _view.CongratulateWinner(superstarName1);
                    }
                    
                }




                void PlayCardAction(List<string> playerHand, List<string> playerHandOpponent,
                    List<string> playerDeck, List<string> playerDeckOpponent,
                    List<string> ringSidePile, List<string> ringSidePileOpponent, List<Card> cardsInfo,
                    string superStarName, string superStarNameOpponent, int playerFortitude, int turno)

                {
                    // Filtrar las cartas que el jugador puede jugar y obtener sus índices

                    var playableCardIndices = playerHand.Select((cardName, index) => new { cardName, index })
                        .Where(x =>
                        {
                            var cardInfo = ConvertToCardInfo(x.cardName, cardsInfo);
                            if (!int.TryParse(cardInfo.Fortitude, out int cardFortitude))
                            {
                                return false; 
                            }

                            return (cardInfo.Types.Contains("Maneuver") || cardInfo.Types.Contains("Action"))
                                   && cardFortitude <= playerFortitude;
                        })
                        .Select(x => x.index)
                        .ToList();

                    // Convertir los índices de las cartas jugables a objetos IViewableCardInfo
                    List<RawDealView.Formatters.IViewableCardInfo> viewableCardsInfo = playableCardIndices
                        .Select(index => { return ConvertToCardInfo(playerHand[index], cardsInfo); }).ToList();

                    // Formatear las cartas jugables para mostrarlas
                    List<string> cardsToDisplay = new List<string>();
                    foreach (var viewableCardInfo in viewableCardsInfo)
                    {
                        var playInfo = new PlayInfo(viewableCardInfo.Title, viewableCardInfo.Types[0],
                            viewableCardInfo.Fortitude, viewableCardInfo.Damage, viewableCardInfo.StunValue,
                            viewableCardInfo, viewableCardInfo.Types[0].ToUpper());
                        string formattedCard = RawDealView.Formatters.Formatter.PlayToString(playInfo);
                        cardsToDisplay.Add(formattedCard);
                    }
                    
                    int cardIndex = _view.AskUserToSelectAPlay(cardsToDisplay);

                    // Verificar si el índice de la carta es valido
                    if (cardIndex >= 0 && cardIndex < playableCardIndices.Count)
                    {
                        // Obtener el nombre de la carta usando el índice original
                        string cardName = playerHand[playableCardIndices[cardIndex]];

                        var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                        var playInfo = new PlayInfo(cardInfo.Title, cardInfo.Types[0], cardInfo.Fortitude,
                            cardInfo.Damage, cardInfo.StunValue, cardInfo, cardInfo.Types[0].ToUpper());

                        string formattedPlayInfo = RawDealView.Formatters.Formatter.PlayToString(playInfo);

              
                        _view.SayThatPlayerIsTryingToPlayThisCard(superStarName, formattedPlayInfo);
                        
                        _view.SayThatPlayerSuccessfullyPlayedACard();
                        
                        playerHand.RemoveAt(playableCardIndices[cardIndex]);

                
                        if (turno == 1)
                        {
                            player1RingArea.Add(cardName);
                        }
                        else
                        {
                            player2RingArea.Add(cardName);
                        }

                        // Si la jugada causa daño al oponente:
                        int damage = CalculateDamage(cardName, cardsInfo);
                        if (damage > 0)
                        {
                            _view.SayThatSuperstarWillTakeSomeDamage(superStarNameOpponent, damage);
                            IncreaseFortitude(damage, turno);
                            for (int i = 0; i < damage; i++)
                            {
                                if (playerDeckOpponent.Count == 0)
                                {
                                    _view.CongratulateWinner(superStarName);
                                    throw new GameEndException(superStarName);
                                }

                                string overturnedCardName = playerDeckOpponent.Last();
                                playerDeckOpponent.RemoveAt(playerDeckOpponent.Count - 1);
                                ringSidePileOpponent.Add(overturnedCardName);

                                var overturnedCardInfo = ConvertToCardInfo(overturnedCardName, cardsInfo);
                                string cardInfoString =
                                    RawDealView.Formatters.Formatter.CardToString(overturnedCardInfo);

                                _view.ShowCardOverturnByTakingDamage(cardInfoString, i + 1, damage);
                            }
                        }
                    }
                    else if (cardIndex == -1)
                    {
                        // El jugador decidio volver al menú anterior sin jugar una carta
                    }
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


                int CalculateDamage(string cardName, List<Card> cardsInfo)
                {
                    // Buscar la informacion de la carta en la lista de cartas
                    var cardInfo = cardsInfo.FirstOrDefault(card => card.Title == cardName);

                    if (cardInfo != null)
                    {
                        // Intentar convertir el valor de daño de la carta a entero
                        if (int.TryParse(cardInfo.Damage, out int damageValue))
                        {
                            return damageValue;
                        }
                    }

                    // Si no se encuentra la carta, no tiene un valor de daño, o no se puede convertir, devolver 0
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
                        cardsToShow.Select(cardName => ConvertToCardInfo(cardName, cardsInfo)).ToList();

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
                    // Buscar la carta en la lista de informacion de cartas
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
                    
                    string currentPlayer = (turn == 1) ? superstarName1 : superstarName2;
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
                            currentRingSide.RemoveAt(cardId);
                            string selectedCard = currentRingSide[cardId];
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
                        
                        List<string> formattedRingSide = currentPlayerRingside.Select(cardName => 
                        {
                            var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                            return RawDealView.Formatters.Formatter.CardToString(cardInfo);
                        }).ToList();
                        
                        List<string> formattedHand = currentPlayerHand.Select(cardName => 
                        {
                            var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                            return RawDealView.Formatters.Formatter.CardToString(cardInfo);
                        }).ToList();
                        
                        
                        _view.SayThatPlayerIsGoingToUseHisAbility("THE UNDERTAKER", superstarAbility);

                        // descartar 2 cartas
                        for (int i = 0; i < 2; i++)
                        {
                            int cardIdToDiscard = _view.AskPlayerToSelectACardToDiscard(formattedHand, 
                                "THE UNDERTAKER", 
                                "THE UNDERTAKER", 2 - i);
                            string discardedCard = currentPlayerHand[cardIdToDiscard];
                            currentPlayerHand.RemoveAt(cardIdToDiscard);
                            currentPlayerRingside.Add(discardedCard);
                        }

                        // elegir carta del ringside
                        int cardIdToRecover = _view.AskPlayerToSelectCardsToPutInHisHand("THE UNDERTAKER", 1, formattedRingSide);
                        string recoveredCard = currentPlayerRingside[cardIdToRecover];
                        currentPlayerRingside.RemoveAt(cardIdToRecover);
                        currentPlayerHand.Add(recoveredCard);
                    }
                }
                
            }
            
            catch (GameEndException ex)
            {
                Console.WriteLine("");
            }
        }
    }
}




        
        
