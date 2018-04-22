﻿#region Copyright
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StoryTests.cs" company="Ian Horswill">
// Copyright (C) 2018 Ian Horswill
//  
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
//  
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PicoSAT;
using static PicoSAT.Language;
using static PicoSAT.Fluents;
using static PicoSAT.Actions;

namespace Tests
{
    [TestClass]
    public class StoryTests
    {
        [TestMethod]
        public void StoryTest()
        {
            var p = new Problem("murder test") { TimeHorizon = 4, MaxFlips = 50000 };
            var cast = new[] { "Fred", "Betty", "Frieda" };

            // FLUENTS

            // alive(a, t) iff a alive at time t
            var alive = Fluent("alive", cast);
            var married = Fluent("married", cast);

            // Everyone is initially alive an unmarried
            foreach (var c in cast)
                p.Assert(alive(c, 0),
                    Not(married(c, 0)));

            var hates = Fluent("hates", cast, cast);
            var loves = Fluent("loves", cast, cast);
            var marriedTo = SymmetricFluent("marriedTo", cast);

            foreach (var c1 in cast)
                foreach (var c2 in cast)
                    p.Assert(Not(marriedTo(c1, c2, 0)),
                        Not(loves(c1, c2, 0)));

            // Love and hate disable one another
            foreach (var agent in cast)
                foreach (var patient in cast)
                    foreach (var t in ActionTimePoints)
                        p.Assert(Deactivate(hates(agent, patient, t)) <= Activate(loves(agent, patient, t)),
                            Deactivate(loves(agent, patient, t)) <= Activate(hates(agent, patient, t)));
            
            // ACTIONS
            // kill(a,b,t) means a kills b at time t
            var kill = Action("kill", cast, cast);
            
            Precondition(kill, (a, b, t) => alive(b, t));
            Precondition(kill, (a, b, t) => alive(a, t));
            Precondition(kill, (a, b, t) => hates(a, b, t));
            Deletes(kill, (a, b, t) => alive(b, t));

            // fallFor(faller, loveInterest, time)
            var fallFor = Action("fallFor", cast, cast);
            Precondition(fallFor, (f, l, t) => Not(loves(f,l, t)));
            Precondition(fallFor, (f, l, t) => alive(f, t));
            Precondition(fallFor, (f, l, t) => alive(l, t));
            Precondition(fallFor, (f, l, t) => f != l);
            Adds(fallFor, (f, l, t) => loves(f, l, t));

            // marry(a, b, t)
            var marry = SymmetricAction("marry", cast);
            Precondition(marry, (a, b, t) => loves(a, b, t));
            Precondition(marry, (a, b, t) => loves(b, a, t));
            Precondition(marry, (a, b, t) => a != b);
            Precondition(marry, (a, b, t) => alive(a, t));
            Precondition(marry, (a, b, t) => alive(b, t));
            Precondition(marry, (a, b, t) => Not(married(a, t)));
            Precondition(marry, (a, b, t) => Not(married(b, t)));
            Adds(marry, (a, b, t) => marriedTo(a, b, t));
            Adds(marry, (a, b, t) => married(a, t));
            Adds(marry, (a, b, t) => married(b, t));

            // You can't marry or fall in love with yourself
            foreach (var t in ActionTimePoints)
            foreach (var c in cast)
            {
                p.Assert(Not(marry(c, c, t)), Not(fallFor(c, c, t)));
            }

            IEnumerable<ActionInstantiation> PossibleActions(int t)
            {
                return Instances(kill, t).Concat(Instances(fallFor, t)).Concat(Instances(marry, t));
            }

            foreach (var t in ActionTimePoints)
                // Exactly one action per time point
                p.AtMost(1, PossibleActions(t));

            // Tragedy strikes
            //foreach (var c in cast)
            //    p.Assert(Not(alive(c, TimeHorizon-1)));

            //p.Assert(married("Fred", 3));

            p.Optimize();

            Console.WriteLine(p.Stats);

            var s =p.Solve();

            foreach (var t in ActionTimePoints)
            {
                Console.Write($"Time {t}: ");
                foreach (var a in PossibleActions(t))
                    if (s[a])
                        Console.Write($"{a}, ");
                Console.WriteLine();
            }
        }
    }
}
