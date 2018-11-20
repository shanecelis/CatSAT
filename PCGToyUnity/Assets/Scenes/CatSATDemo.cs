using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CatSAT;
using static CatSAT.Language;


public class CatSATDemo : MonoBehaviour {
  // Start is called before the first frame update
  void Start() {
    StoryTeller();
  }

  void FirstExercise() {
    CatSAT.Random.SetSeed();
    var p = new Problem("Test");
    var a = (Proposition)"a"; // Add propositions to p
    var b = (Proposition)"b";
    var c = (Proposition)"c";
    var s = p.Solve();
    Debug.Log($"a={s[a]}, b={s[b]}, c={s[c]}");
  }

  void StoryTeller() {
    var p = new Problem("Storyteller demo rebuild");
    // Characters identified by color
    var cast = new[] {"red", "green", "blue"};
    // Character is rich in the first panel
    var rich = Predicate<string>("rich");
    // Character is a prisoner in the second panel
    var caged = Predicate<string>("caged");
    // Character has a sword in the second panel
    var hasSword = Predicate<string>("hasSword");
    // Character is evil
    var evil = Predicate<string>("evil");
    // First argument character stabbed the second
    var stabbed = Predicate<string, string>("stabbed");
    // First argument character loves the second
    var loves = Predicate<string, string>("loves");
    // A tombstone is displayed for the character at the end
    var tombstone = Predicate<string>("tombstone");
    // Some non-evil character is free in the second panel
    var someoneFree = (Proposition) "someoneFree";
    // Panel 1 -> panel 2
    foreach (var x in cast) {
      p.Assert(
               // Povert causes evil in this world
               evil(x) == Not(rich(x)),
               // Only rich people are taken prisoner
               caged(x) > rich(x),
               // You have a sword iff your right and uncaged
               hasSword(x) == (rich(x) & Not(caged(x))),
               // If someone’s not caged, someone’s free
               someoneFree <= Not(caged(x)),
               // No suicide
               Not(stabbed(x,x))
               );
      // You can't kill multiple people
      p.AtMost(1, cast, y => stabbed(x, y));
      foreach (var y in cast) {
        p.Assert(// You need a sword to stab
                 stabbed(x, y) > hasSword(x),
                 // Only stab evil people
                 stabbed(x,y) > evil(y));
      }
    }
    var s = p.Solve();
    Debug.Log("s " + s);
    foreach (var x in cast) {
      Debug.Log($"{x} {Is(s[rich(x)])} rich, "
                + $"{Is(s[caged(x)])} caged, ");
    }
    // Panel 2 -> panel 3
    // foreach (var x in cast) {
    //   foreach (var y in cast) {
    //     p.Assert(// Dead iff you’re stabbed, or caged and not rescued
    //              tombstone(x) <= (caged(x) & evil(y) & Not(stabbed(y, x))),
    //              tombstone(x) <= Not(someoneFree),
    //              tombstone(x) <= stabbed(x, y)
    //              );
    //     foreach (var z in cast)
    //       // You love someone if they rescue you
    //       p.Assert(loves(x, y) <= (caged(x) & stabbed(y, z)));
    //   }
    // }
  }

  public static string Is(bool b) {
    return "is" + (b ? "" : " not");
  }
}
