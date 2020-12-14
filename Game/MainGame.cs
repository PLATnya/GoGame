using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Data;
using System.IO;
using System.Security;
using OpenTK.Windowing.Desktop;
using Rendering;


//мост
//наблюдатель   
namespace Game
{
    enum EEvents
    {
        REMOVE_STONE,
        WIN,
        LOSE,
        SUICIDE,
        ADD_STONE
    }
    struct FMessage
    {
        public ArrayList Data;
    }
    public abstract class BigBrother
    {
        internal abstract void OnNotify(IPerson Person, EEvents Event, FMessage Data);
        public abstract BigBrother Next { get; set; }
    }
    public class ChangeStoneState:BigBrother
    {
        public ChangeStoneState()
        {
            next = null;
        }
        internal override void OnNotify(IPerson Person, EEvents Event,FMessage Message)
        {
            switch (Event)
            {
                case EEvents.REMOVE_STONE:
                    int Kills = (Message.Data[0] as Dictionary<int, int[]>).Count;
                    Person.Score += Kills;
                    Console.WriteLine("You,MFK, killed "+Kills+" enemy stones/");
                    break;
                case EEvents.SUICIDE:
                    Console.WriteLine("Dont kill yourself< think ABOUT your parents!!");
                    break;
                case EEvents.ADD_STONE:
                    break;
            }
        }

        private BigBrother next;
        public override BigBrother Next { get=>next; set=>next = value; }
    }
    
    public abstract class IPerson
    {
        protected abstract int id { get;}
        public abstract IRules Rules { get; set; }
        public abstract int Score { get; set; }
        public abstract bool MakeStep(int x, int y);
        
    }
    public class Player:IPerson
    {
        public BigBrother ObserverHead { get; set; }

        public void AddObserver(BigBrother Brother)
        {
            //BigBrother Buff = Brother;
            //Buff.Next = ObserverHead;
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
        private void Notify(IPerson Person, EEvents Event, FMessage Data)
        {
            BigBrother observer = ObserverHead;
            while (true)
            {
                observer.OnNotify(Person, Event,Data);
                if(observer.Next != null)
                    observer = observer.Next;
                else break;
            }
        }

        protected override int id { get; }
        public override IRules Rules { get; set; }
        public override int Score { get; set; }

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
                            FMessage Message = new FMessage();
                            Message.Data = new ArrayList();
                            Message.Data.Add(Group);
                            Notify(this,EEvents.REMOVE_STONE,Message);
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
            }
            else
            {
                Notify(this,EEvents.SUICIDE,new FMessage());
            }
            return bAnswer;
            
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
            }
        }

        public Player(IRules rules, int id)
        {
            if (Rules == null)
            {
                Rules = rules;
                this.id = id;
            }
            
        }
    }
    public interface IRules
    {
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
        
        private int _size;
        public int[,] Matrix { get; set; }
        
        private int? this[int x, int y]
        {

            get
            {
                if (x < 0 || x >= Matrix.GetLength(0) ||
                    y < 0 || x >= Matrix.GetLength(1))
                    return null;
                return Matrix[x, y];
            }
            set=>  Matrix[x, y] = (int) value;
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
    
    
    
    у
    //TODO: сделать АДАПТЕР из массива игры в красивую графеку
    
    class Program
    {
        static void Main(string[] args)
        {
            
            

            /*
            uint WIDTH = 800;
            uint HEIGTH = 800;
            Graphics graphics = new Graphics(WIDTH,HEIGTH);

            int Offset = 50;
            int PlaceSize = (int)Math.Min(WIDTH, HEIGTH) - Offset;
            
            
            RectangleShape place = new RectangleShape(new Vector2f(PlaceSize, PlaceSize));
            graphics.AddShape(place);
            place.Position = new Vector2f(Offset/2, Offset/2);
            place.FillColor = Color.Green;

            int size = 9;
            float CubeSize = (float)PlaceSize / size;
            
            float RectOffset = Offset / 2 + CubeSize / 2;
            
            for (int i = 0; i < size; i ++)
            {
                
                RectangleShape lineHorizontal = new RectangleShape(new Vector2f(PlaceSize-CubeSize, 1));
                lineHorizontal.FillColor = Color.Black;
                lineHorizontal.Position = new Vector2f(RectOffset,RectOffset + CubeSize * i);
                graphics.AddShape(lineHorizontal);
                
                RectangleShape lineVertical = new RectangleShape( new Vector2f(1,PlaceSize-CubeSize));
                lineVertical.FillColor = Color.Black;
                lineVertical.Position = new Vector2f(RectOffset + CubeSize * i,RectOffset);
                graphics.AddShape(lineVertical);
            }
            
            
            
            
            graphics.Update();*/
        }
    }
}