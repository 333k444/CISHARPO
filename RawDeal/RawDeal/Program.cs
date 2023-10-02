using RawDeal;
using RawDealView;

string folder = "05-SuperstarAbilities";

// Esta vista permite verificar el comportamiento de un test particular.
// El texto en consola saldrá azúl cuando el output sea el esperado y rojo cuando no lo sea. 
// Cuando aparezca texto rojo el programa entrará en "modo manual"

int idTest = 2;
string pathToTest = Path.Combine("data", $"{folder}-Tests", $"{idTest}.txt");
View view = View.BuildManualTestingView(pathToTest); 

// esta vista permite jugar el juego de manera manual
// View view = View.BuildConsoleView();  

string deckFolder = Path.Combine("data", folder);
Game game = new Game(view, deckFolder);
game.Play();