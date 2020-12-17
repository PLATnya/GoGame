using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security;
using OpenTK.Windowing.Desktop;
using Rendering;
using Events;
using OpenTK.Graphics.ES11;
using SFML.Graphics;
using SFML.System;
using SFML.Window;


namespace Game
{
    
    
    public abstract class IPerson
    {
        public abstract bool IsBot();
        protected abstract int id { get;}
        public abstract IRules Rules { get; set; }
        public abstract int Score { get; set; }

        public abstract bool MakeStep(int x, int y);
        internal abstract int PassesCount { get; set; }

    }
    public class Player:IPerson
    {
        internal override int PassesCount { get; set; }
        protected override int id { get; }
        public override IRules Rules { get; set; }
        public override int Score { get; set; }
        public override bool IsBot()
        {
            return false;
        }

        int MakeStatusStone(int X, int Y, ref Dictionary<int, int[]> Group, ref bool bOpen)
        {
            int EnemyId = Rules.Matrix[X, Y];
            Group[(X+Y)*(X+Y+1)/2 + Y] = new int[] { X,Y};
            FStoneWayInfo[] WaysInfo = Rules.IsSurrounded(X, Y, EnemyId, new[] {-1,-2});
            foreach (FStoneWayInfo Info in WaysInfo)
            {
                switch (Info.WayType)
                {
                    case EWays.OPEN:
                        bOpen = true;
                        break;
                    case EWays.FRIEND:
                        int HASH = (Info.X + Info.Y) * (Info.X + Info.Y + 1) / 2 + Info.Y;
                        if (!Group.ContainsKey(HASH)) MakeStatusStone(Info.X,Info.Y,ref Group,ref bOpen);
                        break;
                }
            }

            return EnemyId;
        }

        void FillByKey(Dictionary<int, int[]> Group, int Key)
        {
            foreach (int[] Var in Group.Values)
            {
                Rules.Matrix[Var[0], Var[1]] = Key;
            }
        }
        public void OcupateEnemy()
        {
            
            for (int i = 0; i < Rules.Matrix.GetLength(0); i++)
            {
                for (int j = 0; j < Rules.Matrix.GetLength(1); j++)
                {
                    if (Rules.Matrix[i, j] != 0 && Rules.Matrix[i, j] != id 
                                                &&Rules.Matrix[i,j]>-1)
                    {
                        bool bOpen = false;
                        Dictionary<int,int[]> Group = new Dictionary<int,int[]>();
                        int EnemyID = MakeStatusStone(i,j,ref Group,ref bOpen);

                        if (bOpen)
                        {
                            FillByKey(Group,-2 - EnemyID);
                        }
                        else
                        {
                            FillByKey(Group,-1);
                            int Kills = Group.Count;
                            Score += Kills;
                            Console.WriteLine("You,MFK, killed "+Kills+" enemy stones/");
                            /*
                            FMessage Message = new FMessage();
                            Message.Data = new ArrayList();
                            Message.Data.Add(Group);
                            Notify(this,EEvents.REMOVE_STONE,Message);*/
                        }   
                        Group.Clear();
                        //уничтожаем по групам(меняем значение на нейтральное)
                        //таки образом эти значения не будут трогатся при повторном обходе
                    } 
                }
            }
        }

        public void ClearField()
        {
            for (int i = 0; i < Rules.Matrix.GetLength(0); i++)
            {
                for (int j = 0; j < Rules.Matrix.GetLength(0); j++)
                {
                    if (Rules.Matrix[i, j] == -1) Rules.Matrix[i, j] = 0;
                    else if (Rules.Matrix[i, j] < -1)
                    {
                        Rules.Matrix[i, j] = -(Rules.Matrix[i, j] + 2);
                    }
                }    
            }
        }
        public override bool MakeStep(int x, int y)
        {
            
            bool bAnswer = Rules.Check(GetHashCode(),x,y);
            if (bAnswer)
            {
                OcupateEnemy();
                ClearField();
                
                

                PassesCount = 0;
                Rules.NextStep();
            }
            return bAnswer;
            
        }
        
        //
        public void Pass( )
        {
            PassesCount += 1;
            Rules.NextStep();
        }
        public override int GetHashCode()
        {
            return id;
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

            if (Rules == null)
            {
                Rules = rules;
                id = 1;
                Rules.AddPlayer(this);
            }
        }

        public Player(IRules rules, int id)
        {
            if (Rules == null)
            {
                Rules = rules;
                this.id = id;
                Rules.AddPlayer(this);
            }
            
        }
    }
    public interface IRules
    {


        public void NextStep();
        public void AddPlayer(IPerson Person);
        public int[,] Matrix { get; set; }
        public int SIZE { get; set; }
        public bool Check(int id, int x, int y);
        public FStoneWayInfo[] IsSurrounded(int x, int y,int id, int[] Alt);
    }
    public enum EWays
    {
        OPEN,
        FRIEND,
        ALT
    }
    public struct FStoneWayInfo
    {
        public readonly EWays WayType;
        public readonly int X;
        public readonly int Y;

        public FStoneWayInfo(EWays wayType, int x, int y)
        {
            WayType = wayType;
            X = x;
            Y = y;
        }
    }
    public class GoRules:IRules
    {
        public List<IPerson> Persons;
        private int ActivePersonIndex;
        private int _size;
        private bool bIsFinished;
        

        public void NextStep()
        {
            int Passes = 0;
            foreach (IPerson Pers in Persons)
            {
                Passes += Pers.PassesCount;
            }
            if (Passes >= 2) bIsFinished = true;
            if (!bIsFinished)
            {
                ActivePersonIndex++;
                if (ActivePersonIndex >= Persons.Count) ActivePersonIndex = 0;
            }
            else
            {
                
                PlayerField.GetGame().MainFrame.Window.Close();
                //TODO: post end game logic
            }
        }

        public IPerson GetActivePerson()
        {
            return Persons[ActivePersonIndex];}

        public void AddPlayer(IPerson Person)
        {
            Persons.Add(Person);
        }

        public int[,] Matrix { get; set; }
        
        public int? this[int x, int y]
        {

            get
            {
                if (x < 0 || x >= Matrix.GetLength(0) || y < 0 || y >= Matrix.GetLength(1)) return null; 
                return Matrix[x, y];
            }
            set=>  Matrix[x, y] =  (int)value;
        }
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
            Persons = new List<IPerson>(2);
        }

        public void SetMatrix(int[,] Matrix)
        {
            this.Matrix = Matrix;
        }
        
        public bool Check(int id,int x, int y)
        {
            int? Element = this[x, y];
            if (Element == null) return false;

            if (Element == 0)
            {
                bool bSurrounded = false;

                FStoneWayInfo[] WaysInfo = IsSurrounded(x, y, id, new int[]{});
                if (WaysInfo.Length == 0) bSurrounded = true;
                if (!bSurrounded) this[x, y] = id;
                return !bSurrounded;
            }
            return false;
            //TODO: можно ли поставить проверяем при ходе в рулсах(КО)
            //(самоубиственное, но можно забрать камни опонента и правило КО)
        }

        public FStoneWayInfo[] IsSurrounded(int x, int y, int id, int[] Alt)
        {

            List<FStoneWayInfo> Answer = new List<FStoneWayInfo>();
            int?[,] Around =
            {
                {x,y+1},
                {x,y-1},
                {x+1,y},
                {x-1,y}
            };
            
            bool Enemy = true;
            for (int i = 0; i < Around.GetLength(0); i++)
            {
                int? Value = this[(int)Around[i,0],(int)Around[i,1]];
                if (Value == 0) Answer.Add(new FStoneWayInfo(EWays.OPEN, (int) Around[i, 0], (int) Around[i, 1]));  
                else if (Value == id) Answer.Add(new FStoneWayInfo(EWays.FRIEND, (int) Around[i, 0], (int) Around[i, 1]));
                else
                {
                    foreach (int Var in Alt)
                    {
                        if(Value == Var) Answer.Add(new FStoneWayInfo(EWays.ALT, (int) Around[i, 0], (int) Around[i, 1]));  
                    }
                }
            }
            return Answer.ToArray();
        }

        public void Print()
        {
            for (int i = 0; i < Matrix.GetLength(0); i++)
            {
                for (int j = 0; j<Matrix.GetLength(1); j++)
                {
                    if (Matrix[i, j] < 0)
                        Console.Write(Matrix[i, j]);
                    else Console.Write(" "+ Matrix[i, j]);
                }
                Console.Write('\n');
            }
        }
    }
    //сосотояние
    
    
    public class PlayerField
    {
        private PlayerField() { }
        private static PlayerField _instance;

        public Graphics MainFrame { get; set; }
        public static PlayerField GetGame()
        {
            if (_instance == null) {
                _instance = new PlayerField();
                _instance.MainFrame = new Graphics(800,600);
            }
            return _instance;
        }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            
            
            PlayerField Field = PlayerField.GetGame();
            Graphics Frame = Field.MainFrame;
            GoRules Rules = new GoRules(9);
            
            
            Player FirstPlayer = new Player(Rules,1);
            Player SecondPlayer = new Player(Rules,2);


            Goban Go = new Goban(Rules);
            Frame.AddGroup(Go);



            Frame.Window.MouseMoved += (obj, e) => { Frame.MousePosition = new Vector2f(e.X, e.Y); };
            
            Frame.Window.MouseButtonPressed += (obj, e) =>
            {
                if (e.Button == Mouse.Button.Left)
                {
                    if (!Rules.GetActivePerson().IsBot())
                    {
                        int Id = Rules.GetActivePerson().GetHashCode() - 1;
                        Console.WriteLine(Id.ToString());
                        Go.SelectedStone = new Stone(Go.CubeSize / 4f);
                        Color CustomColor = new Color();
                        CustomColor.A = 255;
                        CustomColor.R = (byte) (255 * Id);
                        CustomColor.G = (byte) (255 * Id);
                        CustomColor.B = (byte) (255 * Id);
                        Go.SelectedStone.Shape.FillColor = CustomColor;
                        Go.SelectedStone.ChangeState(new GrabedState(Go.SelectedStone));
                        Go.AddShape(Go.SelectedStone);
                    }
                }
            };
            Frame.Window.MouseButtonReleased += (obj, e) =>
            {
                if (e.Button == Mouse.Button.Left)
                {
                    if (!Rules.GetActivePerson().IsBot())
                    {
                        if (Go.SelectedStone != null)
                        {
                            (bool bCanPlace, int X, int Y) = Go.CanTookStone(e.X, e.Y);
                            bool bStepDone = Rules.GetActivePerson().MakeStep(X, Y);
                            if (bStepDone)
                            {
                                Go.SelectedStone.ChangeState(null);
                                Go.TookOnGrid(Go.SelectedStone, X, Y);
                                Go.CheckConfirmity();

                            }
                            else
                            {
                                Go.RemoveShape(Go.SelectedStone);
                            }
                            Go.SelectedStone = null;
                        }
                    }
                }
            };
            
            
            
            
            
            
            
            List<Keyboard.Key> KeyBuffer = new List<Keyboard.Key>(4);
            Frame.Window.KeyPressed+= (obj, e) =>
            {
                KeyBuffer.Add(e.Code);

                if (Rules.GetActivePerson().GetType() == typeof(Player))
                {
                    switch (e.Code)
                    {
                        
                    }
                    
                }
            };
            Frame.Window.KeyReleased += (obj, e) =>
            {
                KeyBuffer.Remove(e.Code);
                switch (e.Code)
                {
                    
                }
            };
            
            Frame.Update();
            
        }
    }
}