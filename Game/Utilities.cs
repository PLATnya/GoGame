using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks.Dataflow;
using Game;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using PrimitiveType = SFML.Graphics.PrimitiveType;

namespace Rendering
{


    abstract class StoneState
    {
        protected GameObject Obj;

        public StoneState(GameObject obj)
        {
            this.Obj = obj;
        }
        public abstract void OnPress();
        public abstract void OnReleased();
        public virtual void UpdateState(){}
    }

    class GrabedState : StoneState
    {
        public GrabedState(GameObject obj) : base(obj)
        {
            
        }

        public override void OnPress()
        {
            
        }

        public override void OnReleased()
        {
            
        }

        public override void UpdateState()
        {
            Obj.Shape.Position = (Vector2f) Mouse.GetPosition();
        }
    }
    
    
    
    
    
    
    class GameObject
    {
        public Shape Shape { get; }
        public GameObject(Shape shae)
        {
            Shape = shae;
        }
        public void Update()
        {
            PlayerField.GetGame().MainFrame.Window.Draw(Shape);
        }
    }

    class Stone : GameObject
    {
        private StoneState State;
        public Stone(Shape shae) : base(shae)
        {
        }
        public void ChangeState(StoneState state)
        {
            this.State = state;
        }
    }
    
    
    internal abstract class Group
    {
        private List<GameObject> Objects;

        public void AddShape(GameObject Obj )
        {
            Objects.Add(Obj);
        }

        public virtual void Update()
        {
            foreach (var VARIABLE in Objects)
            {
                VARIABLE.Update();
            }
        }
    }

    class GridGroup:Group
    {
        private BigBrother Observer;
        internal GameObject[,] Grid;
        public GridGroup(int Size)
        {
            Grid = new GameObject[Size, Size];
        }

        public void AddShape(GameObject Obj,int x,int y)
        {
            Grid[x, y] = Obj;
            base.AddShape(Obj);
        }
        //TODO: група синхронится с игроком
        public void SyncPlayer(Player player)
        {
            Observer = player.ObserverHead;
        }
        
    }

    class GobanGridGroup:Group
    {
        private GridGroup Player1;
        private GridGroup Player2;

        private GameObject[,] Grid;
        public GobanGridGroup()
        {
            if (Player1.Grid.GetLength(0) == Player2.Grid.GetLength(0))
            {
                Grid = new GameObject[Player1.Grid.GetLength(0), Player1.Grid.GetLength(0)];
                for (int i = 0; i < Grid.GetLength(0); i++)
                {
                    for (int j = 0; j < Grid.GetLength(1); j++)
                    {
                        Grid[i, j] = Player1.Grid[i, j] != null ? Player1.Grid[i, j] : Player2.Grid[i, j];
                    }
                }
            }
            else throw new Exception("Different sizes");
        }
        public override void Update()
        {
            Player1.Update();
            Player2.Update();
        }
    }
    
    
    public class Graphics
    {
        private List<Group> Groups;
        private RenderWindow _window;
        public RenderWindow Window
        {
            get => _window;
        }
        public Graphics(uint x,uint y)
        {
            _window = new RenderWindow(new VideoMode(x,y),"dsfdf");
            _window.SetVerticalSyncEnabled(true);
            _window.Closed += (obj, e) => { _window.Close(); };

            Groups = new List<Group>();
        }
        public void Update()
        {
            while (_window.IsOpen)
            {
                _window.DispatchEvents();


                foreach (var VARIABLE in Groups)
                {
                    VARIABLE.Update();
                }
                _window.Display(); 
            }
            
        }

    }
    
}