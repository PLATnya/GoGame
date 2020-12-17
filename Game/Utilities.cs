using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks.Sources;
using Events;
using Game;
using Rendering;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using PrimitiveType = SFML.Graphics.PrimitiveType;

namespace Rendering
{

    enum EInput
    {
        PRESSED,
        RELEASED
    }
    abstract class StoneState
    {
        protected GameObject Obj;

        public StoneState(GameObject obj)
        {
            this.Obj = obj;
        }
        public virtual void UpdateState(){}
    }

    class GrabedState : StoneState
    {
        public GrabedState(GameObject obj) : base(obj)
        {
        }

        public override void UpdateState()
        {

            Obj.Shape.Position = PlayerField.GetGame().MainFrame.MousePosition;
        }
    }


    public abstract class ScreenObject
    {
        public NotifierSub Subscribe;
        public abstract void Update();
    }
    
    public class GameObject:ScreenObject
    {
        public Shape Shape { get; }
        public GameObject(Shape shae)
        {
            Shape = shae;
        }
        public override void Update()
        {
            PlayerField.GetGame().MainFrame.Window.Draw(Shape);
        }
    }

    public class TextObject:ScreenObject
    {
        public Text Text;

        public TextObject(string Str, Font Fnt)
        {
            Text = new Text(Str, Fnt);
        }
        public override void Update()
        {
            PlayerField.GetGame().MainFrame.Window.Draw(Text);
        }
    }

    public class ButtonObject : TextObject
    {
        public Shape BackShape;
        public ButtonObject(string Str, Font Fnt) : base(Str, Fnt)
        {
            BackShape = new RectangleShape(new Vector2f(Text.GetGlobalBounds().Width, Text.GetGlobalBounds().Height));
            BackShape.FillColor = Color.Magenta;
        }

        public void SetPosition(int x, int y)
        {
            Text.Position = new Vector2f(x, y);
            BackShape.Position = new Vector2f(x, y);
        }
        public override void Update()
        {
            
            PlayerField.GetGame().MainFrame.Window.Draw(BackShape);
            base.Update();
        }
    }
    class Stone : GameObject
    {
        private StoneState State;
        public Stone(float R) : base(new CircleShape(R))
        {
            Shape.Origin = new Vector2f(R, R);
        }
        public void ChangeState(StoneState state)
        {
            this.State = state;
        }

        public override void Update()
        {
            base.Update();
            if(State!=null) State.UpdateState();
        }
    }
    
    
    public abstract class Group
    {
        private List<ScreenObject> Objects;

        public void AddShape(ScreenObject Obj )
        {
            if (Objects == null) Objects = new List<ScreenObject>();
            Objects.Add(Obj);
        }
        
      
        public void RemoveShape(GameObject Obj)
        {
            Objects.Remove(Obj);
        }

        public virtual void Update()
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                Objects[i].Update();
            }
            
        }

        public virtual void Notify(EEvents Event,FMessage Data)
        {
            for (int i = 0; i < Objects.Count; i++)
            {
                if (Objects[i].Subscribe != null)
                {
                    Objects[i].Subscribe.Notify(Event, Data);
                }
            }
            
        }
    }

 
   
    class Goban : Group
    {
        private GoRules Rules;
        private GameObject[,] Grid;
        private TextObject[] ScoreObjects; 
        private float RectOffset;
        public float CubeSize;
        
        public Stone SelectedStone;
        
        
        public (bool,int,int) CanTookStone(int X, int Y)
        {
            int CellX=0, CellY = 0;
            if (X < RectOffset || Y < RectOffset) return (false,CellX,CellY);
            if (X >= RectOffset+CubeSize*Rules.SIZE || Y >= RectOffset+CubeSize*Rules.SIZE) return (false,CellX,CellY);
            
            CellX = (int)Math.Round(((X - RectOffset) / CubeSize));
            CellY = (int)Math.Round(((Y - RectOffset) / CubeSize));
            return (true,CellX,CellY);
        }
        
        public void TookOnGrid(GameObject Obj, int X, int Y)
        {
            Grid[X, Y] = Obj;
            Obj.Shape.Position = new Vector2f(RectOffset + CubeSize * X, RectOffset + CubeSize * Y);
        }

        public void CheckConfirmity()
        {
            for (int i = 0; i < Rules.Matrix.GetLength(0); i++)
            {
                for (int j = 0; j < Rules.Matrix.GetLength(1); j++)
                {
                    if (Rules.Matrix[i, j] == 0 && Grid[i, j] != null)
                    {
                        RemoveShape(Grid[i,j]);
                        Grid[i, j] = null;
                    }
                }
            }

            UpdateScore();
        }
        
        public void UpdateScore()
        {

            ScoreObjects[0].Text.DisplayedString = "Score: " + Rules.Persons[0].Score.ToString();
            ScoreObjects[1].Text.DisplayedString = "Score: " + Rules.Persons[1].Score.ToString();
        }
        public Goban(GoRules Rules)
        {
            this.Rules = Rules;
            Grid = new GameObject[Rules.SIZE, Rules.SIZE];
            ScoreObjects = new TextObject[2];
            
            Vector2u Size = PlayerField.GetGame().MainFrame.Window.Size;
            GameObject Back = new GameObject(new RectangleShape((Vector2f)Size));
            Back.Shape.FillColor = Color.Blue;
            
            AddShape(Back);


            
            
            
            
            int MinDimension = (int)Math.Min(Size.X, Size.Y);
            int Offset = MinDimension/20;
            int PlaceSize = MinDimension - Offset;
            
            
            GameObject place = new GameObject(new RectangleShape(new Vector2f(PlaceSize, PlaceSize)));
            
            place.Subscribe = new NotifierSub(place);
           
            place.Subscribe.AddObserver(new OnPressMessage(((i, i1) =>
            {
                
                if (!Rules.GetActivePerson().IsBot())
                {
                    int Id = Rules.GetActivePerson().GetHashCode() - 1;
                    Console.WriteLine(Id.ToString());
                    SelectedStone = new Stone(CubeSize / 4f);
                    Color CustomColor = new Color();
                    CustomColor.A = 255;
                    CustomColor.R = (byte) (255 * Id);
                    CustomColor.G = (byte) (255 * Id);
                    CustomColor.B = (byte) (255 * Id); 
                    SelectedStone.Shape.FillColor = CustomColor;
                    SelectedStone.ChangeState(new GrabedState(SelectedStone));
                    AddShape(SelectedStone);
                }
                
                
            })));
            
            place.Subscribe.AddObserver(new OnReleaseMessage(((x, y) =>
            {
                if (!Rules.GetActivePerson().IsBot())
                {
                    if (SelectedStone != null)
                    {
                        (bool bCanPlace, int X, int Y) = CanTookStone(x, y);
                        bool bStepDone = Rules.GetActivePerson().MakeStep(X, Y);
                        if (bStepDone)
                        {
                            SelectedStone.ChangeState(null);
                            TookOnGrid(SelectedStone, X, Y);
                            CheckConfirmity();

                        }
                        else
                        {
                            RemoveShape(SelectedStone);
                        }
                        SelectedStone = null;
                    }
                }
            })));
            
            AddShape(place);
            place.Shape.Position = new Vector2f(Offset/2, Offset/2);
            place.Shape.FillColor = Color.Green;

            int size = Rules.SIZE;
            CubeSize = (float)PlaceSize / size;
            RectOffset = Offset / 2 + CubeSize / 2;
            for (int i = 0; i < size; i ++)
            {
                RectangleShape lineHorizontal = new RectangleShape(new Vector2f(PlaceSize-CubeSize, 1));
                lineHorizontal.FillColor = Color.Black;
                lineHorizontal.Position = new Vector2f(RectOffset,RectOffset + CubeSize * i);
                AddShape(new GameObject(lineHorizontal));
                
                RectangleShape lineVertical = new RectangleShape( new Vector2f(1,PlaceSize-CubeSize));
                lineVertical.FillColor = Color.Black;
                lineVertical.Position = new Vector2f(RectOffset + CubeSize * i,RectOffset);
                AddShape(new GameObject(lineVertical));
            }

            
            ScoreObjects[0] = new TextObject("Score: 0", new Font("C:/CurrentProjects/LetsGo/Game/FONT.ttf"));
            ScoreObjects[0].Text.FillColor = Color.Black;
            ScoreObjects[1] = new TextObject("Score: 0", new Font("C:/CurrentProjects/LetsGo/Game/FONT.ttf"));
            ScoreObjects[1].Text.FillColor = Color.White;
            Vector2f TextSize = new Vector2f(ScoreObjects[0].Text.GetGlobalBounds().Width,
                ScoreObjects[0].Text.GetGlobalBounds().Height);
            ScoreObjects[0].Text.Position = (Vector2f) PlayerField.GetGame().MainFrame.Window.Size - TextSize;
            
            TextSize = new Vector2f(ScoreObjects[1].Text.GetGlobalBounds().Width,
                ScoreObjects[1].Text.GetGlobalBounds().Height);
            ScoreObjects[1].Text.Position = new Vector2f( (PlayerField.GetGame().MainFrame.Window.Size.X - TextSize.X),0);
            AddShape(ScoreObjects[0]);
            AddShape(ScoreObjects[1]);

            
            GameObject Pass = new GameObject(new RectangleShape(new Vector2f(50,50)));
            //ButtonObject Pass = new ButtonObject("Pass", new Font("C:/CurrentProjects/LetsGo/Game/FONT.ttf"));
            Pass.Shape.Position = new Vector2f(750,300);
            Pass.Subscribe = new NotifierSub(Pass);
            Pass.Subscribe.AddObserver(new OnPressMessage(((i, i1) =>
            {
                Console.WriteLine("dfd");
                if(!Rules.GetActivePerson().IsBot()) ((Player)Rules.GetActivePerson()).Pass();
            })));
            AddShape(Pass);
        }
        

        
    }
    
    
    
    public class Graphics
    {
        //TODO: переместить всю логику нажатий и тд в надлюдателя, добавить каждой групе подписку
        private List<Group> Groups;
        private RenderWindow _window;
        public Vector2f MousePosition;
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

            _window.MouseButtonPressed += (sender, args) =>
            {
                if (args.Button == Mouse.Button.Left)
                {
                    foreach (var VARIABLE in Groups)
                    {
                        FMessage Message = new FMessage();
                        Message.Dict = new Dictionary<string, int>();
                        Message.Dict["X"] = args.X;
                        Message.Dict["Y"] = args.Y;
                        VARIABLE.Notify(EEvents.PRESS, Message);
                    }
                }
            };
            _window.MouseButtonReleased += (sender, args) =>
            {
                if (args.Button == Mouse.Button.Left) 
                {
                    
                    foreach (var VARIABLE in Groups)
                    {
                        FMessage Message = new FMessage();
                        Message.Dict = new Dictionary<string, int>();
                        Message.Dict["X"] = args.X;
                        Message.Dict["Y"] = args.Y;
                        VARIABLE.Notify(EEvents.RELEASE, Message);
                    }
                }
            };
            _window.MouseMoved += (sender, args) => {MousePosition = new Vector2f(args.X, args.Y); };
        }

        public void AddGroup(Group NewGroup)
        {
            Groups.Add(NewGroup);
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

namespace Events
{
    public enum EEvents
    {
        PRESS,
        RELEASE,
        
    }
    public struct FMessage
    {
        public Dictionary<string, int> Dict;
        public ArrayList Data;
    }
    public abstract class BigBrother
    {
        internal abstract void OnNotify(ScreenObject Obj, EEvents Event, FMessage Data);
        public abstract BigBrother Next { get; set; }
    }
    public class OnPressMessage:BigBrother
    {
        public delegate void Pressed(int x, int y);

        private Pressed OnPress;
        public OnPressMessage(Pressed OnPress)
        {
            this.OnPress = OnPress;
        }
        internal override void OnNotify(ScreenObject Obj, EEvents Event,FMessage Message)
        {
            if (Event == EEvents.PRESS)
            {
               
                int[,] Bounds = new int[2, 2];
                if (Obj.GetType() == typeof(GameObject))
                {
                    GameObject GameObj = (GameObject) Obj;
                    Vector2f Pos = (GameObj.Shape.Position - GameObj.Shape.Origin);
                    Bounds[0, 0] = (int)Pos.X;
                    Bounds[0, 1] = (int) Pos.X + (int)GameObj.Shape.GetGlobalBounds().Width;
                    Bounds[1, 0] = (int)Pos.Y;
                    Bounds[1, 1] = (int) Pos.X + (int)GameObj.Shape.GetGlobalBounds().Height;
                }else if (Obj.GetType() == typeof(TextObject))
                {
                    TextObject GameObj = (TextObject) Obj;
                    Vector2f Pos = (GameObj.Text.Position - GameObj.Text.Origin);
                    Bounds[0, 0] = (int)Pos.X;
                    Bounds[0, 1] = (int) Pos.X + (int)GameObj.Text.GetGlobalBounds().Width;
                    Bounds[1, 0] = (int)Pos.Y;
                    Bounds[1, 1] = (int) Pos.X + (int)GameObj.Text.GetGlobalBounds().Height;
                }
                
                
                int X = Message.Dict["X"];
                int Y = Message.Dict["Y"];

                if (X >= Bounds[0, 0] && X <= Bounds[0, 1] && Y >= Bounds[1, 0] && Y <= Bounds[1, 1])
                {
                    OnPress(X,Y);
                }
                
            }
            
        }

        private BigBrother next;
        public override BigBrother Next { get=>next; set=>next = value; }
        
    }
    public class OnReleaseMessage:BigBrother
    {
        public delegate void Released(int x, int y);

        private Released OnRelease;
        public OnReleaseMessage(Released OnRelease)
        {
            this.OnRelease = OnRelease;
        }
        internal override void OnNotify(ScreenObject Obj, EEvents Event,FMessage Message)
        {
            if (Event == EEvents.RELEASE)
            {
                
                int X = Message.Dict["X"];
                int Y = Message.Dict["Y"];

                OnRelease(X,Y);
                
            }
            
        }

        private BigBrother next;
        public override BigBrother Next { get=>next; set=>next = value; }
        
    }
    public class NotifierSub
    {
        private ScreenObject Self;
        public BigBrother f { get; set; }
        public BigBrother ObserverHead { get; set; }

        public NotifierSub(ScreenObject Obj)
        {
            Self = Obj;
        }
        public void AddObserver(BigBrother Brother)
        {
            //BigBrother Buff = Brother;
            //Buff.Next = ObserverHead;
            
            //((OnPressMessage) Brother).Pressed = Make; 
            if(ObserverHead!=null)
                Brother.Next = ObserverHead;
            ObserverHead = Brother;
            //ObserverHead = Buff;
        }

        public void Remove(ref BigBrother Brother)
        {
            if (ReferenceEquals(ObserverHead, Brother))
            {
                ObserverHead = Brother.Next; 
                Brother.Next = null;
                return; 
            }
            BigBrother current = ObserverHead; 
            while (current != null) 
            { 
                if (current.Next == Brother) 
                { 
                    current.Next = Brother.Next; 
                    Brother.Next = null; 
                    return;
                }
                current = current.Next;
            }   
        }
        public void Notify(EEvents Event, FMessage Data)
        {
            BigBrother observer = ObserverHead;
            while (true)
            {
                observer.OnNotify(Self,Event,Data);
                if(observer.Next != null)
                    observer = observer.Next;
                else break;
            }
        }
    }
}

namespace Commands
{
    public abstract class Command
    {
        private Player ReceiverPlayer;
        public abstract void Execute();
        public Command(Player ReceiverPlayer)
        {
            this.ReceiverPlayer = ReceiverPlayer;
        }
    }
    
    public class PassCommand : Command
    {
        public PassCommand(Player player) : base(player)
        {
        }

        public override void Execute()
        {
            
        }
    }
    public class StepCommand : Command
    {
        public StepCommand(Player player) : base(player)
        {
        }

        public override void Execute()
        {
            
        }
    }

    public class UndoCommand : Command
    {
        public UndoCommand(Player ReceiverPlayer) : base(ReceiverPlayer)
        {
        }

        public override void Execute()
        {
            
        }
    }
}

//Invoker(выполнятель) - Graphics

    
    //сделать шаг
    //удалить
    //пасс
