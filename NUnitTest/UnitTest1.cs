
using NUnit.Framework;
using Game.Logic;



namespace NUnitTest
{
    public class Tests
    {
        public GoRulesBase RulesBase;
        public Player player;
        public Player player2;
        [SetUp]
        public void Setup()
        {
            RulesBase = new GoRulesBase(9);
            player = new Player(RulesBase);
            player2 = new Player(RulesBase,2);
        }
        [Test]
        public void Compare_Reference_To_Rules()
        {
            Assert.IsTrue(ReferenceEquals(RulesBase,player.RulesBase));
        }
        [Test]
        public void Make_Wrong_Step()
        {
            Assert.IsFalse(player.MakeStep(10,-1));
        }
        
        [Test]
        public void Suicide_Step()
        {
            player.MakeStep(1, 0);
            player.MakeStep(0, 1);
            player.MakeStep(1, 2);
            player.MakeStep(2, 1);
            Assert.False(player2.MakeStep(1,1));

            player.MakeStep(0, 3);
            player.MakeStep(1, 4);
            player.MakeStep(0, 5);
            Assert.False(player2.MakeStep(0,4));
        }
        [Test]
        public void Not_Suicide_Step()
        {
            player.MakeStep(1, 0);
            player.MakeStep(0, 1);
            player.MakeStep(1, 2);
            player2.MakeStep(2, 1);
            Assert.True(player2.MakeStep(1,1));
        }

        [Test]
        public void Can_Escape_Step()
        {
            player.MakeStep(1, 0);
            player.MakeStep(0, 1);
            player.MakeStep(1, 2);
            /*0 1
             *1 2
             *0 1
             */ 
            Assert.True(player2.MakeStep(1,1));
        }

        [Test]
        public void Field_1()
        {
            
            player.MakeStep(1, 0);
            player.MakeStep(0, 1);
            player2.MakeStep(1, 1);
            player.MakeStep(2, 1);
            RulesBase.PrintMatrix();
            player.MakeStep(1, 2);
            RulesBase.PrintMatrix();
            Assert.AreEqual(1,player.Score);
        }

        [Test]
        public void Field2()
        {
            RulesBase.Matrix = new int[7, 7]
            {
                {0, 1, 1, 1, 1, 0, 0},
                {1, 0, 2, 2, 2, 1, 0},
                {1, 1, 1, 1, 1, 1, 0},
                {0, 2, 2, 1, 2, 2, 1},
                {0, 0, 0, 0, 1, 1, 0},
                {0, 0, 0, 0, 0, 0, 0},
                {0, 0, 0, 0, 0, 0, 0}
            };
            player.OccupyEnemy();
            RulesBase.PrintMatrix();
            Assert.AreEqual(-4,RulesBase[1,2]);
            Assert.AreEqual(-4,RulesBase[1,3]);
            Assert.AreEqual(-4,RulesBase[1,4]);
            Assert.AreEqual(-4,RulesBase[3,1]);
            Assert.AreEqual(-4,RulesBase[3,2]);
            Assert.AreEqual(-1,RulesBase[3,4]);
            Assert.AreEqual(-1,RulesBase[3,5]);
        }
    }
}