using System;
using System.ComponentModel.Design;
using System.Security;
using SFML.Graphics;
using SFML.Window;


namespace Game
{
    public abstract class IPerson
    {
        public abstract IRules Rules { get; set; }
        public abstract int Score { get; set; }
        public abstract void MakeStep(int x, int y);
        
    }

    public class Player:IPerson
    {
        public override IRules Rules { get; set; }
        public override int Score { get; set; }
        public override void MakeStep(int x, int y)
        {
            bool res = Rules.Check(GetHashCode(),x,y);
            //TODO:make some reaction
        }
        public override int GetHashCode()
        {
            return 1;
        }

        public override bool Equals(object? obj)
        {
            if (obj.GetType() != typeof(Player)) return false;
            Player obj2 = (Player) obj;
            if (!ReferenceEquals(Rules, obj2.Rules)) return false;
            if (obj2.GetHashCode() != GetHashCode()) return false;
            return true;
        }

        public Player(IRules rules)
        {
            if(Rules == null) Rules = rules;
        }
    }

    public interface IRules
    {
        public int[,] Matrix { get; set; }
        public int SIZE { get; set; }
        public bool Check(int id, int x, int y);
    }
    
    
    public class GoRules:IRules
    {
        private int _size;
        public int[,] Matrix { get; set; }

        public int SIZE
        {
            get => _size;
            set
            {
                if (value!=_size)
                {
                    Matrix = new int[value,value];
                }
                _size = value;
            }
        }

        public GoRules(int size)
        {
            SIZE = size;
        }
        public bool Check(int id,int x, int y)
        {
            return false;
            //TODO:make go rules
        }
    }
    
    class Game
    {
        private Game()
        { }
        private static Game _instance;
        private static RenderWindow _window;
        public RenderWindow Window => _window;
        

        public static Game GetGame()
        {
            if (_instance == null) {
                _instance = new Game();
                _window = new RenderWindow(new VideoMode(800,600),"dsfdf");
                _window.SetVerticalSyncEnabled(true);
                _window.Closed += (obj, e) => { _window.Close(); };
                /*_window.Resized += (obj, e) =>
                {
                    _window.SetView(new View(new FloatRect(0, 0, e.Width, e.Height)));
                };*/
            }
            return _instance;
        }
        
        
            
    }

    class Program
    {
        static void Main(string[] args)
        {

            Game main_game = Game.GetGame();
            RenderWindow window = main_game.Window;
            CircleShape circle = new CircleShape(20);
            circle.FillColor = Color.Green;
            while (window.IsOpen)
            {
                window.DispatchEvents();
                window.Clear(Color.Blue);
                window.Draw(circle);
                window.Display();
            }
            
          
        }
    }
}