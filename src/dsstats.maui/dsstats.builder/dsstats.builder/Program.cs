using dsstats.shared;
using dsstats.shared.DsFen;

namespace dsstats.builder;

class Program
{
    static void Main(string[] args)
    {
        Thread.Sleep(1000);
        string fen = "2:Terran;10q15/9q16/8q17/7q18/6q15q3/5q17w2/4q18qe1/3q19qqw/2q20qq1/1q20qq2/q20qq3/19eqq4/19qw5/18qq6/17qq7/16qq8/15qq9/14qq10/12eqq11/11qqw12/11qq13/5q4qq14/6wqqqq15/7eqq16/8w17|26/19z6/26/26/26/26/26/26/26/21d4/26/19d6/26/15f1d8/26/15d10/26/13d12/2z23/11d14/26/9d16/26/26/26";
        var cmdr = Commander.None;
        int team = 0;
        var spawn = new SpawnDto();
        DsFen.ApplyFen(fen, spawn, out cmdr, out team);
        DsBuilder.Build(spawn, cmdr, team, dry: true);
    }
}
