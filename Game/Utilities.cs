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
            if (Rules.bIsFinished) Console.WriteLine("FINISH");
            ScoreObjects[0].Text.DisplayedString = "Score: " + Rules.Persons[0].Score.ToString();
            ScoreObjects[1].Text.DisplayedString = "Score: " + Rules.Persons[1].Score.ToString();
        }
        
        public Goban(GoRules Rules)
        {
            this.Rules = Rules;
            Grid = new GameObject[Rules.SIZE, Rules.SIZE];
            ScoreObjects = new TextObject[2];

            Vector2u Size = PlayerField.GetGame().MainFrame.Window.Size;
            GameObject Back = new GameObject(new RectangleShape((Vector2f) Size));
            Back.Shape.FillColor = Color.Blue;

            AddShape(Back);






            int MinDimension = (int) Math.Min(Size.X, Size.Y);
            int Offset = MinDimension / 20;
            int PlaceSize = MinDimension - Offset;


            GameObject place = new GameObject(new RectangleShape(new Vector2f(PlaceSize, PlaceSize)));

            place.Subscribe = new NotifierSub(place);

            place.Subscribe.AddObserver(new OnPressMessage(((i, i1) =>
            {

                if (!Rules.GetActivePerson().IsBot()&&!Rules.bIsFinished)
                {
                    int Id = Rules.GetActivePerson().GetHashCode() - 1;
                    Console.WriteLine(Id.ToString());
                    SelectedStone = new Stone(CubeSize / 3f);
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
            place.Shape.Position = new Vector2f(Offset / 2, Offset / 2);
            place.Shape.FillColor = Color.Green;

            int size = Rules.SIZE;
            CubeSize = (float) PlaceSize / size;
            RectOffset = Offset / 2 + CubeSize / 2;
            for (int i = 0; i < size; i++)
            {
                RectangleShape lineHorizontal = new RectangleShape(new Vector2f(PlaceSize - CubeSize, 1));
                lineHorizontal.FillColor = Color.Black;
                lineHorizontal.Position = new Vector2f(RectOffset, RectOffset + CubeSize * i);
                AddShape(new GameObject(lineHorizontal));

                RectangleShape lineVertical = new RectangleShape(new Vector2f(1, PlaceSize - CubeSize));
                lineVertical.FillColor = Color.Black;
                lineVertical.Position = new Vector2f(RectOffset + CubeSize * i, RectOffset);
                AddShape(new GameObject(lineVertical));
            }


            ScoreObjects[0] = new TextObject("Score: 0", new Font("C:/CurrentProjects/LetsGo/Game/FONT.ttf"));
            ScoreObjects[0].Text.FillColor = Color.Black;
            ScoreObjects[1] = new TextObject("Score: 0", new Font("C:/CurrentProjects/LetsGo/Game/FONT.ttf"));
            ScoreObjects[1].Text.FillColor = Color.White;
            Vector2f TextSize = new Vector2f(ScoreObjects[0].Text.GetGlobalBounds().Width,
                ScoreObjects[0].Text.GetGlobalBounds().Height + 20);
            ScoreObjects[0].Text.Position = (Vector2f) PlayerField.GetGame().MainFrame.Window.Size - TextSize;

            TextSize = new Vector2f(ScoreObjects[1].Text.GetGlobalBounds().Width,
                ScoreObjects[1].Text.GetGlobalBounds().Height);
            ScoreObjects[1].Text.Position =
                new Vector2f((PlayerField.GetGame().MainFrame.Window.Size.X - TextSize.X), 0);
            AddShape(ScoreObjects[0]);
            AddShape(ScoreObjects[1]);

            
            
            TextObject PassText = new TextObject("Pass", new Font("C:/CurrentProjects/LetsGo/Game/FONT.ttf"));
            GameObject Pass = new GameObject(new RectangleShape(new Vector2f(
                PassText.Text.GetGlobalBounds().Width * 1.5f, PassText.Text.GetGlobalBounds().Height * 1.5f)));

            //ButtonObject Pass = new ButtonObject("Pass", new Font("C:/CurrentProjects/LetsGo/Game/FONT.ttf"));
            Pass.Shape.Position = new Vector2f(650, 300);
            PassText.Text.Position = new Vector2f(650, 300);
            PassText.Text.FillColor = Color.Magenta;
            Pass.Subscribe = new NotifierSub(Pass);
            Pass.Subscribe.AddObserver(new OnPressMessage(((i, i1) =>
            {
                Console.WriteLine("dfd");
                if (!Rules.GetActivePerson().IsBot()) ((Player) Rules.GetActivePerson()).Pass();

                if (Rules.bIsFinished)
                {
                    string EndText = "";
                    if (Rules.Persons[0].Score < Rules.Persons[1].Score) EndText = "WHITE WIN";
                    else if (Rules.Persons[0].Score > Rules.Persons[1].Score) EndText = "BLACK WIN";
                    else EndText = "DRAW";
                    TextObject FinishText =
                        new TextObject(EndText, new Font("C:/CurrentProjects/LetsGo/Game/FONT.ttf"));
                    FinishText.Text.Position = new Vector2f(250, 250);
                    FinishText.Text.FillColor = Color.Blue;
                    
                    AddShape(FinishText);
                }
                Pass.Shape.FillColor = Color.Green;
            })));
            Pass.Subscribe.AddObserver(new OnReleaseMessage(((i, i1) => { Pass.Shape.FillColor = Color.White; })));
            AddShape(Pass);
            AddShape(PassText);
        }
    }
    public class Graphics
    {
        
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
                    for (int i = 0; i < Groups.Count; i++)
                    {
                        FMessage Message = new FMessage();
                        Message.Dict = new Dictionary<string, int>();
                        Message.Dict["X"] = args.X;
                        Message.Dict["Y"] = args.Y;
                        Groups[i].Notify(EEvents.PRESS, Message);
                    }
                    
                }
            };
            _window.MouseButtonReleased += (sender, args) =>
            {
                if (args.Button == Mouse.Button.Left)
                {
                    for (int i = 0; i < Groups.Count; i++)
                    {
                        FMessage Message = new FMessage();
                        Message.Dict = new Dictionary<string, int>();
                        Message.Dict["X"] = args.X;
                        Message.Dict["Y"] = args.Y;
                        Groups[i].Notify(EEvents.RELEASE, Message);
                    }
                    
                }
            };
            _window.KeyPressed += (sender, args) =>
            {
                switch (args.Code)
                {
                    case Keyboard.Key.F:
                        PlayerField.GetGame().StartGame(9);
                        break;
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
                int X = Message.Dict["X"];
                int Y = Message.Dict["Y"];

                bool bContain = false;
                //int[,] Bounds = new int[2, 2];
                if (Obj.GetType() == typeof(GameObject))
                {
                    GameObject GameObj = (GameObject) Obj;
                    bContain = GameObj.Shape.GetGlobalBounds().Contains(X, Y);
                    
                   
                }else if (Obj.GetType() == typeof(TextObject))
                {
                    TextObject GameObj = (TextObject) Obj;
                    bContain = GameObj.Text.GetGlobalBounds().Contains(X, Y);

                }

                if (bContain)
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
            if(ObserverHead!=null)
                Brother.Next = ObserverHead;
            ObserverHead = Brother;
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

