using System;
using System.Data;
using NUnit.Framework;
using Game;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using NuGet.Frameworks;

namespace NUnitTest
{
    public class Tests
    {
        public GoRules Rules;
        public Player player;
        public Player player2;

        
        [SetUp]
        public void Setup()
        {
            

            Rules = new GoRules(9);
            player = new Player(Rules);
            BigBrother ChangeStone = new ChangeStoneState();
            player.AddObserver(ChangeStone);
            player2 = new Player(Rules,2);
            player2.AddObserver(new ChangeStoneState());

        }
        [Test]
        public void TestField()
        {
            PlayerField df = PlayerField.GetGame();
            
            Assert.AreEqual(df.MainFrame,PlayerField.GetGame().MainFrame);
        }
        
        [Test]
        public void Compare_Reference_To_Rules()
        {
            
            Assert.IsTrue(ReferenceEquals(Rules,player.Rules));
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
            Rules.Print();
            player.MakeStep(1, 2);
            Rules.Print();
            //Assert.AreEqual(-1,Rules.Matrix[1,1]);
            Assert.AreEqual(1,player.Score);
        }

        [Test]
        public void Field2()
        {
            Rules.SIZE = 7;
            Rules.SetMatrix(new int[7,7]
            {
                {0,1,1,1,1,0,0},
                {1,0,2,2,2,1,0},
                {1,1,1,1,1,1,0},
                {0,2,2,1,2,2,1},
                {0,0,0,0,1,1,0},
                {0,0,0,0,0,0,0},
                {0,0,0,0,0,0,0}
            });
            player.OcupateEnemy();
            Rules.Print();
            Assert.AreEqual(-4,Rules.Matrix[1,2]);
            Assert.AreEqual(-4,Rules.Matrix[1,3]);
            Assert.AreEqual(-4,Rules.Matrix[1,4]);
            Assert.AreEqual(-4,Rules.Matrix[3,1]);
            Assert.AreEqual(-4,Rules.Matrix[3,2]);
            Assert.AreEqual(-1,Rules.Matrix[3,4]);
            Assert.AreEqual(-1,Rules.Matrix[3,5]);
        }


        [Test]
        public void Test_Match()
        {
            
            
            Assert.Pass();
            //player.MakeStep()
        }
    }
}