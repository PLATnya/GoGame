using System;
using System.Collections.Generic;
using Events;
using Game;
using Rendering;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using EventType = Events.EventType;

namespace Rendering
{
    abstract class StoneState
    {
        protected readonly GameObject Obj;

        protected StoneState(GameObject obj)
        {
            Obj = obj;
        }

        public abstract void UpdateState();
    }
    internal class GrabedState : StoneState
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
        public readonly Text Text;

        public TextObject(string str, Font fnt)
        {
            Text = new Text(str, fnt);
        }
        public override void Update()
        {
            PlayerField.GetGame().MainFrame.Window.Draw(Text);
        }
    }
    class Stone : GameObject
    {
        private StoneState _state;
        public Stone(float r) : base(new CircleShape(r))
        {
            Shape.Origin = new Vector2f(r, r);
        }
        public void ChangeState(StoneState state)
        {
            _state = state;
        }

        public override void Update()
        {
            base.Update();
            _state?.UpdateState();
        }
    }
    public abstract class Group
    {
        private List<ScreenObject> _objects;

        protected void AddShape(ScreenObject obj )
        {
            if (_objects == null) _objects = new List<ScreenObject>();
            _objects.Add(obj);
        }


        protected void RemoveShape(GameObject obj)
        {
            _objects.Remove(obj);
        }

        public void Update()
        {
            foreach (var t in _objects)
            {
                t.Update();
            }
        }

        public void Notify(EventType eventType,FMessage data)
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                if (_objects[i].Subscribe != null)
                {
                    _objects[i].Subscribe.Notify(eventType, data);
                }
            }
            
        }
    }
    internal class Goban : Group
    {
        private readonly GoRulesBase _rulesBase;
        private readonly GameObject[,] _grid;
        private readonly TextObject[] _scoreObjects; 
        private readonly float _rectOffset;
        private readonly float _cubeSize;
        private Stone _selectedStone;
        private (bool,int,int) CanTookStone(int x, int y)
        {
            int cellX=0, cellY = 0;
            if (x < _rectOffset || y < _rectOffset) return (false,cellX,cellY);
            if (x >= _rectOffset+_cubeSize*_rulesBase[0] || y >= _rectOffset+_cubeSize*_rulesBase[0]) return (false,cellX,cellY);
            
            cellX = (int)Math.Round(((x - _rectOffset) / _cubeSize));
            cellY = (int)Math.Round(((y - _rectOffset) / _cubeSize));
            return (true,cellX,cellY);
        }

        private void TookOnGrid(GameObject obj, int x, int y)
        {
            _grid[x, y] = obj;
            obj.Shape.Position = new Vector2f(_rectOffset + _cubeSize * x, _rectOffset + _cubeSize * y);
        }

        private void CheckConfirmity()
        {
            for (int i = 0; i < _rulesBase[0]; i++)
            {
                for (int j = 0; j < _rulesBase[1]; j++)
                {
                    if (_rulesBase[i, j] == 0 && _grid[i, j] != null)
                    {
                        RemoveShape(_grid[i,j]);
                        _grid[i, j] = null;
                    }
                }
            }

            UpdateScore();
        }

        private void UpdateScore()
        {
            if (_rulesBase.IsFinished) Console.WriteLine("FINISH");
            _scoreObjects[0].Text.DisplayedString = "Score: " + _rulesBase.Persons[0].Score.ToString();
            _scoreObjects[1].Text.DisplayedString = "Score: " + _rulesBase.Persons[1].Score.ToString();
        }
        
        public Goban(GoRulesBase rulesBase)
        {
            _rulesBase = rulesBase;
            _grid = new GameObject[rulesBase[0], rulesBase[0]];
            _scoreObjects = new TextObject[2];

            Vector2u windowSize = PlayerField.GetGame().MainFrame.Window.Size;
            GameObject back = new GameObject(new RectangleShape((Vector2f) windowSize));
            back.Shape.FillColor = Color.Blue;
            AddShape(back);
            int minDimension = (int) Math.Min(windowSize.X, windowSize.Y);
            int offset = minDimension / 20;
            int placeSize = minDimension - offset;
            GameObject place = new GameObject(new RectangleShape(new Vector2f(placeSize, placeSize)));

            place.Subscribe = new NotifierSub(place);

            place.Subscribe.AddObserver(new OnPressMessage(((i, i1) =>
            {
                
                if (!rulesBase.GetActivePerson().IsBot&&!rulesBase.IsFinished)
                {
                    int id = rulesBase.GetActivePerson().Id - 1;
                    Console.WriteLine(id.ToString());
                    _selectedStone = new Stone(_cubeSize / 3f);
                    Color customColor = new Color();
                    customColor.A = 255;
                    customColor.R = (byte) (255 * id);
                    customColor.G = (byte) (255 * id);
                    customColor.B = (byte) (255 * id);
                    _selectedStone.Shape.FillColor = customColor;
                    _selectedStone.ChangeState(new GrabedState(_selectedStone));
                    AddShape(_selectedStone);
                }
            })));

            place.Subscribe.AddObserver(new OnReleaseMessage(((x, y) =>
            {
                if (!rulesBase.GetActivePerson().IsBot)
                {
                    if (_selectedStone != null)
                    {
                        (bool canPlace, int xStone, int yStone) = CanTookStone(x, y);
                        bool isStepDone = rulesBase.GetActivePerson().MakeStep(xStone, yStone);
                        if (isStepDone)
                        {
                            _selectedStone.ChangeState(null);
                            TookOnGrid(_selectedStone, xStone, yStone);
                            CheckConfirmity();
                        }
                        else
                        {
                            RemoveShape(_selectedStone);
                        }
                        _selectedStone = null;
                    }
                }
            })));

            AddShape(place);
            place.Shape.Position = new Vector2f(offset / 2.0f, offset / 2.0f);
            place.Shape.FillColor = Color.Green;

            int size = rulesBase[0];
            
            _cubeSize = (float) placeSize / size;
            _rectOffset = offset / 2.0f + _cubeSize / 2;
            for (int i = 0; i < size; i++)
            {
                RectangleShape lineHorizontal = new RectangleShape(new Vector2f(placeSize - _cubeSize, 1));
                lineHorizontal.FillColor = Color.Black;
                lineHorizontal.Position = new Vector2f(_rectOffset, _rectOffset + _cubeSize * i);
                AddShape(new GameObject(lineHorizontal));

                RectangleShape lineVertical = new RectangleShape(new Vector2f(1, placeSize - _cubeSize));
                lineVertical.FillColor = Color.Black;
                lineVertical.Position = new Vector2f(_rectOffset + _cubeSize * i, _rectOffset);
                AddShape(new GameObject(lineVertical));
            }


            _scoreObjects[0] = new TextObject("Score: 0", new Font("C:/CurrentProjects/LetsGo/Game/FONT.ttf"));
            _scoreObjects[0].Text.FillColor = Color.Black;
            _scoreObjects[1] = new TextObject("Score: 0", new Font("C:/CurrentProjects/LetsGo/Game/FONT.ttf"));
            _scoreObjects[1].Text.FillColor = Color.White;
            Vector2f textSize = new Vector2f(_scoreObjects[0].Text.GetGlobalBounds().Width,
                _scoreObjects[0].Text.GetGlobalBounds().Height + 20);
            _scoreObjects[0].Text.Position = (Vector2f) PlayerField.GetGame().MainFrame.Window.Size - textSize;

            textSize = new Vector2f(_scoreObjects[1].Text.GetGlobalBounds().Width,
                _scoreObjects[1].Text.GetGlobalBounds().Height);
            _scoreObjects[1].Text.Position =
                new Vector2f((PlayerField.GetGame().MainFrame.Window.Size.X - textSize.X), 0);
            AddShape(_scoreObjects[0]);
            AddShape(_scoreObjects[1]);

            
            
            TextObject passText = new TextObject("Pass", new Font("C:/CurrentProjects/LetsGo/Game/FONT.ttf"));
            GameObject pass = new GameObject(new RectangleShape(new Vector2f(
                passText.Text.GetGlobalBounds().Width * 1.5f, passText.Text.GetGlobalBounds().Height * 1.5f)));

            //ButtonObject Pass = new ButtonObject("Pass", new Font("C:/CurrentProjects/LetsGo/Game/FONT.ttf"));
            pass.Shape.Position = new Vector2f(650, 300);
            passText.Text.Position = new Vector2f(650, 300);
            passText.Text.FillColor = Color.Magenta;
            pass.Subscribe = new NotifierSub(pass);
            pass.Subscribe.AddObserver(new OnPressMessage(((i, i1) =>
            {
                if (!rulesBase.GetActivePerson().IsBot) ((Player) rulesBase.GetActivePerson()).Pass();

                if (rulesBase.IsFinished)
                {
                    string endText;
                    if (rulesBase.Persons[0].Score < rulesBase.Persons[1].Score) endText = "WHITE WIN";
                    else if (rulesBase.Persons[0].Score > rulesBase.Persons[1].Score) endText = "BLACK WIN";
                    else endText = "DRAW";
                    TextObject finishText = new TextObject(endText, new Font("C:/CurrentProjects/LetsGo/Game/FONT.ttf"));
                    finishText.Text.Position = new Vector2f(250, 250);
                    finishText.Text.FillColor = Color.Blue;
                    
                    AddShape(finishText);
                }
                pass.Shape.FillColor = Color.Green;
            })));
            pass.Subscribe.AddObserver(new OnReleaseMessage(((i, i1) => { pass.Shape.FillColor = Color.White; })));
            AddShape(pass);
            AddShape(passText);
        }
    }
    public class Graphics
    {
        
        private readonly List<Group> _groups;
        public Vector2f MousePosition;
        public RenderWindow Window { get; }

        public Graphics(uint x,uint y)
        {
            Window = new RenderWindow(new VideoMode(x,y),"dsfdf");
            Window.SetVerticalSyncEnabled(true);
            Window.Closed += (obj, e) => { Window.Close(); };

            _groups = new List<Group>();

            Window.MouseButtonPressed += (sender, args) =>
            {
                if (args.Button != Mouse.Button.Left) return;
                foreach (var t in _groups)
                {
                    FMessage message = new FMessage();
                    message.Dict = new Dictionary<string, int>();
                    message.Dict["X"] = args.X;
                    message.Dict["Y"] = args.Y;
                    t.Notify(EventType.Press, message);
                }
            };
            Window.MouseButtonReleased += (sender, args) =>
            {
                if (args.Button != Mouse.Button.Left) return;
                foreach (var t in _groups)
                {
                    FMessage message = new FMessage();
                    message.Dict = new Dictionary<string, int>();
                    message.Dict["X"] = args.X;
                    message.Dict["Y"] = args.Y;
                    t.Notify(EventType.Release, message);
                }
            };
            Window.KeyPressed += (sender, args) =>
            {
                switch (args.Code)
                {
                    case Keyboard.Key.F:
                        PlayerField.GetGame().StartGame(9);
                        break;
                }
            };
            Window.MouseMoved += (sender, args) => {MousePosition = new Vector2f(args.X, args.Y); };
        }

        public void AddGroup(Group newGroup)
        {
            _groups.Add(newGroup);
        }
        public void Update()
        {
            while (Window.IsOpen)
            {
                Window.DispatchEvents();
                foreach (var variable in _groups)
                {
                    variable.Update();
                }
                Window.Display(); 
            }
            
        }

    }
}

namespace Events
{
    public enum EventType
    {
        Press,
        Release,
        
    }
    public struct FMessage
    {
        public Dictionary<string, int> Dict;
    }
    public abstract class BigBrother
    {
        internal abstract void OnNotify(ScreenObject obj, EventType eventType, FMessage data);
        public abstract BigBrother Next { get; set; }
    }
    public class OnPressMessage:BigBrother
    {
        public delegate void Pressed(int x, int y);

        private readonly Pressed _onPress;
        public OnPressMessage(Pressed onPress)
        {
            _onPress = onPress;
        }
        internal override void OnNotify(ScreenObject obj, EventType eventType,FMessage data)
        {
            if (eventType == EventType.Press)
            {
                int x = data.Dict["X"];
                int y = data.Dict["Y"];

                bool contains = false;
                if (obj.GetType() == typeof(GameObject))
                {
                    GameObject gameObj = (GameObject) obj;
                    contains = gameObj.Shape.GetGlobalBounds().Contains(x, y);
                    
                   
                }else if (obj.GetType() == typeof(TextObject))
                {
                    TextObject gameObj = (TextObject) obj;
                    contains = gameObj.Text.GetGlobalBounds().Contains(x, y);
                }
                if (contains)
                {
                    _onPress(x,y);
                }
                
            }
            
        }

        private BigBrother _next;
        public override BigBrother Next { get=>_next; set=>_next = value; }
        
    }
    public class OnReleaseMessage:BigBrother
    {
        public delegate void Released(int x, int y);

        private readonly Released _onRelease;
        public OnReleaseMessage(Released onRelease)
        {
            _onRelease = onRelease;
        }
        internal override void OnNotify(ScreenObject obj, EventType eventType,FMessage data)
        {
            if (eventType == EventType.Release)
            {
                int x = data.Dict["X"];
                int y = data.Dict["Y"];

                _onRelease(x,y);
            }
        }
        private BigBrother _next;
        public override BigBrother Next { get=>_next; set=>_next = value; }
    }
    public class NotifierSub
    {
        private ScreenObject Self;
        private BigBrother ObserverHead { get; set; }
        public NotifierSub(ScreenObject obj)
        {
            Self = obj;
        }
        public void AddObserver(BigBrother brother)
        {
            if(ObserverHead!=null)
                brother.Next = ObserverHead;
            ObserverHead = brother;
        }
        public void Notify(EventType eventType, FMessage data)
        {
            BigBrother observer = ObserverHead;
            while (true)
            {
                observer.OnNotify(Self,eventType,data);
                if(observer.Next != null)
                    observer = observer.Next;
                else break;
            }
        }
    }
}

