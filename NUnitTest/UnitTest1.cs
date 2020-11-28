using System;
using NUnit.Framework;
using Game;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace NUnitTest
{
    public class Tests
    {
        public GoRules Rules;
        public Player player;
        [SetUp]
        public void Setup()
        {
            Rules = new GoRules(9);
            player = new Player(Rules);
                
        }

        public void Print(IRules rules)
        {
            foreach (int var in rules.Matrix)
            {
                Console.Write(var);
            }
        }    
        
        [Test]
        public void Test1()
        {
            Print(Rules);
            Assert.IsTrue(ReferenceEquals(Rules,player.Rules));
        }
    }
}