#nullable enable
using System;
using System.Collections.Generic;
using Game.Rendering;



namespace Game.Logic
{
    
    public abstract class PersonBase
    {
        public readonly bool IsBot;
        public readonly int Id;
        public readonly RulesBase RulesBase;
        public int Score { get; protected set; }
        public abstract bool MakeStep(int x, int y);
        internal int PassesCount { get; set; }

        protected PersonBase(int id, bool isBot, RulesBase rulesBase)
        {
            Id = id;
            IsBot = isBot;
            RulesBase = rulesBase;
        }
    }
    
    public class Player:PersonBase
    {
        private int MakeStatusStone(int x, int y, ref Dictionary<int, int[]> group, ref bool bOpen)
        {
            int enemyId = (int)RulesBase[x, y];
            group[(x+y)*(x+y+1)/2 + y] = new[] { x,y};
            StoneWayInfo[] waysInfo = RulesBase.IsSurrounded(x, y, enemyId, new[] {-1,-2});
            foreach (StoneWayInfo info in waysInfo)
            {
                switch (info.WayTypeType)
                {
                    case WayType.Open:
                        bOpen = true;
                        break;
                    case WayType.Friend:
                        int hash = (info.X + info.Y) * (info.X + info.Y + 1) / 2 + info.Y;
                        if (!group.ContainsKey(hash)) MakeStatusStone(info.X,info.Y,ref group,ref bOpen);
                        break;
                }
            }
            return enemyId;
        }
        private void FillByKey(Dictionary<int, int[]> group, int key)
        {
            foreach (int[] var in group.Values)
            {
                RulesBase[var[0], var[1]] = key;
            }
        }
        public void OccupyEnemy()
        {
            
            for (int i = 0; i < RulesBase[0]; i++)
            {
                for (int j = 0; j < RulesBase[1]; j++)
                {
                    if (RulesBase[i, j] != 0 && RulesBase[i, j] != Id 
                                                &&RulesBase[i,j]>-1)
                    {
                        bool isOpen = false;
                        Dictionary<int,int[]> group = new Dictionary<int,int[]>();
                        int enemyId = MakeStatusStone(i,j,ref group,ref isOpen);
                        if (isOpen)
                        {
                            FillByKey(group,-2 - enemyId);
                        }
                        else
                        {
                            FillByKey(group,-1);
                            int kills = group.Count;
                            Score += kills;
                            Console.WriteLine("You,MFK, killed "+kills+" enemy stones/");
                        }   
                        group.Clear();
                    } 
                }
            }
        }
        private void ClearField()
        {
            for (int i = 0; i < RulesBase[0]; i++)
            {
                for (int j = 0; j < RulesBase[1]; j++)
                {
                    if (RulesBase[i, j] == -1) RulesBase[i, j] = 0;
                    else if (RulesBase[i, j] < -1)
                    {
                        RulesBase[i, j] = -(RulesBase[i, j] + 2);
                    }
                }    
            }
        }
        public override bool MakeStep(int x, int y)
        {
            bool bAnswer = RulesBase.Check(Id,x,y);
            if (bAnswer)
            {
                OccupyEnemy();
                ClearField();
                PassesCount = 0;
                RulesBase.NextStep();
            }
            return bAnswer;
        }
        public void Pass( )
        {
            PassesCount = 1;
            RulesBase.NextStep();
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object? obj)
        {
            if (obj?.GetType() != GetType()) return false;
            Player obj2 = (Player) obj; 
            if (!Equals(RulesBase, obj2.RulesBase)) return false;
            if (obj2.GetHashCode() != GetHashCode()) return false;
            return true;
        }
        public Player(RulesBase rulesBase):base(1,false, rulesBase)
        {
            RulesBase.AddPlayer(this);
        }
        public Player(RulesBase rulesBase, int id):base(id,false, rulesBase)
        {
            RulesBase.AddPlayer(this);
        }
    }
    public abstract class RulesBase
    {
        public abstract void NextStep();
        public abstract void AddPlayer(PersonBase personBase);
        public int[,] Matrix {private get; set; }
        
        
        
        protected RulesBase(int size)
        {
            Matrix = new int[size,size];
            
        }
        public int this[int x] => Matrix.GetLength(x);

        public int? this[int x, int y]
        {
            get
            {
                if (x < 0 || x >= Matrix.GetLength(0) || y < 0 || y >= Matrix.GetLength(1)) return null; 
                return Matrix[x, y];
            }
            set
            {
                if (value != null) Matrix[x, y] = (int) value;
            }
        }
        //public int XDim => Matrix.GetLength(0);
        //public int YDim => Matrix.GetLength(1);
        public abstract bool Check(int id, int x, int y);
        public abstract StoneWayInfo[] IsSurrounded(int x, int y,int id, int[] alt);
    }
    public enum WayType
    {
        Open,
        Friend,
        Alt
    }
    public readonly struct StoneWayInfo
    {
        public readonly WayType WayTypeType;
        public readonly int X;
        public readonly int Y;
        public StoneWayInfo(WayType wayTypeType, int x, int y)
        {
            WayTypeType = wayTypeType;
            X = x;
            Y = y;
        }
    }
    public class GoRulesBase:RulesBase
    {
        public readonly List<PersonBase> Persons;
        private int _activePersonIndex;
        public bool IsFinished;
        public override void NextStep()
        {
            int passes = 0;
            foreach (PersonBase pers in Persons)
            {
                passes += pers.PassesCount;
            }
            if (passes >= 2) IsFinished = true;
            if (!IsFinished)
            {
                _activePersonIndex++;
                if (_activePersonIndex >= Persons.Count) _activePersonIndex = 0;
            }
            else
            {
                Console.WriteLine(Persons[0].Score.ToString()+" "+ Persons[1].Score.ToString());
                //TODO: post end game logic... its fucking hard
            }
        }
        public PersonBase GetActivePerson()
        {
            return Persons[_activePersonIndex];}
        public override void AddPlayer(PersonBase personBase)
        {
            Persons.Add(personBase);
        }
        public GoRulesBase(int size):base(size)
        {
            _activePersonIndex = 0;
            Persons = new List<PersonBase>(2);
        }
        public override bool Check(int id,int x, int y)
        {
            int? element = this[x, y];
            if (element == null) return false;
            if (element == 0)
            {
                bool bSurrounded = false;
                StoneWayInfo[] waysInfo = IsSurrounded(x, y, id, new int[]{});
                if (waysInfo.Length == 0) bSurrounded = true;
                if (!bSurrounded) this[x, y] = id;
                return !bSurrounded;
            }
            return false;
            //TODO: "Ko" rule
        }
        public override StoneWayInfo[] IsSurrounded(int x, int y, int id, int[] alt)
        {

            List<StoneWayInfo> answer = new List<StoneWayInfo>();
            int?[,] around =
            {
                {x,y+1},
                {x,y-1},
                {x+1,y},
                {x-1,y}
            };

            for (int i = 0; i < around.GetLength(0); i++)
            {
                int? value = this[(int)around[i,0],(int)around[i,1]];
                if (value == 0) answer.Add(new StoneWayInfo(WayType.Open, (int) around[i, 0], (int) around[i, 1]));  
                else if (value == id) answer.Add(new StoneWayInfo(WayType.Friend, (int) around[i, 0], (int) around[i, 1]));
                else
                {
                    foreach (int var in alt)
                    {
                        if(value == var) answer.Add(new StoneWayInfo(WayType.Alt, (int) around[i, 0], (int) around[i, 1]));  
                    }
                }
            }
            return answer.ToArray();
        }
        public void PrintMatrix()
        {
            for (int i = 0; i < this[0]; i++)
            {
                for (int j = 0; j<this[1]; j++)
                {
                    if (this[i, j] < 0)
                        Console.Write(this[i, j]);
                    else Console.Write(" "+ this[i, j]);
                }
                Console.Write('\n');
            }
        }
    }
    
    
    public class PlayerField
    {
        private PlayerField() { }
        private static PlayerField? _instance;
        public Graphics MainFrame { get; private set; }
        private RulesBase RulesBase { get; set; }
        public static PlayerField GetGame()
        {
            if (_instance == null) {
                _instance = new PlayerField();
                _instance.MainFrame = new Graphics(800,600);
            }
            return _instance;
        }
        public void StartGame(int size)
        {
            GetGame().RulesBase = new GoRulesBase(size);
            
            Player firstPlayer = new Player(RulesBase,1);
            Player secondPlayer = new Player(RulesBase,2);
            
            Goban goGame = new Goban((GoRulesBase)GetGame().RulesBase);
            GetGame().MainFrame.AddGroup(goGame);
            GetGame().MainFrame.Update();
        }
    }
    static class Game
    {
        static void Main(string[] args)
        {
            PlayerField field = PlayerField.GetGame();
            field.StartGame(9);
        }
    }
}