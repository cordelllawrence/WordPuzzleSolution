using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;


namespace wordament
{
    class Program
    {
        public const int MAX_LENGTH = 12;
        public const int MIN_LENGTH = 5;
        public static readonly string DictionarySample = "https://raw.githubusercontent.com/dwyl/english-words/master/words.txt";
        public static readonly string DictionaryFile = "dictionary.txt";
        public static readonly string CombinationFile = "paths.txt";

        public static readonly ValueTuple<int, int>[] Moves = new[] { (0, -1), (1, -1), (1, 0), (1, 1), (0, 1), (-1, 1), (-1, 0), (-1, -1) };
        public static readonly List<int[]> PathMatrix = new List<int[]> { new int[] { 01, 02, 03, 04 }, new int[] { 05, 06, 07, 08 }, new int[] { 09, 10, 11, 12 }, new int[] { 13, 14, 15, 16 } };
        public static List<int[]> PossiblePaths = new List<int[]>();
        public static SortedSet<string> dictionary = new SortedSet<string>();

        public static List<string> FoundWords = new List<string>();

        public static string Puzzle = "t p a f u u e i s s e l e a r p";

        static void Main(string[] args)
        {
            if(!File.Exists(Program.CombinationFile))
                CreatePathFile();

            Program.ReadPathFile();

            if(!File.Exists(Program.DictionaryFile)) 
            {
                Console.WriteLine($"Cannot find dictionary file ... fetching generic file from '{DictionarySample}'");
                using(var client = new System.Net.WebClient())
                {
                    File.WriteAllText(DictionaryFile, new StreamReader(client.OpenRead(DictionarySample)).ReadToEnd());
                }
            }
            
            Program.LoadDirctionary();

            // ******* Filter Dictionary
            //dictionary = new SortedSet<string>(dictionary.Distinct());

            string input = "";
            var watch = new Stopwatch();

            while (input != "end")
            {
                Console.Write("Puzzle string: ");
                input = Console.ReadLine().ToLower().Trim();
                
                if(string.IsNullOrEmpty(input) == false)
                    Puzzle = input;

                if(Puzzle.Split(" ").Length != 16)
                {
                    Console.WriteLine(">>> ERROR - Puzzle is not 16 chars ... this app is designed for 4x4 boggle style puzzles");
                }
                else
                {
                    //watch.Restart();
                    Program.MapWords2();
                    //watch.Stop();
                    //Console.WriteLine(watch.Elapsed.ToString("G"));

                    Console.WriteLine("*********************************");
                    foreach (var word in FoundWords
                        .Distinct()
                        .OrderBy(word => word.Length)
                        //.OrderBy(word => word)
                        )
                    {
                        Console.WriteLine(word);
                        Console.WriteLine();
                    }
                    Console.WriteLine("*********************************");

                    FoundWords.Clear();
                }
            }
            
            Console.WriteLine("... program execution completed ...");
        }

        public static void MapWords()
        {
            var blocks = Program.Puzzle.Split(" "); 
            
            // load complete dictionary file into memory
            foreach (var path in Program.PossiblePaths)
            {
                var word = "";
                foreach (var step in path)
                    word += blocks[step - 1];

                if (dictionary.Contains(word))
                {
                    Console.WriteLine(word);
                    Program.FoundWords.Add(word);
                }
            }
        }

        public static void MapWords2()
        {
            var possiblePaths = PossiblePaths;
            string[] puzzle = Puzzle.Split(" ");

            Func<int[], string> buildWord = (list) => { var w = ""; foreach (int i in list) w += puzzle[i-1]; return w; };

            var words = 
                possiblePaths
                .Select(p => buildWord(p)).AsParallel()
                .Where(p => dictionary.Contains(p)).AsParallel();

            FoundWords = words.ToList();

        }

        static void LoadDirctionary()
        {
            Console.Write("Loading all words from dictionary ... ");
            using (var reader = new StreamReader(File.Open(Program.DictionaryFile, FileMode.Open, FileAccess.Read)))
            {
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    
                    if(line.Length >= MIN_LENGTH && line.Length <= MAX_LENGTH)
                        Program.dictionary.Add(line);
                }
            }
            Console.WriteLine("completed.");
        }

        static void ExploreAllPossiblePaths()
        {
            Console.Write("Exploring all possible paths ... ");
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    var start = new Node((j, i), new Stack<int>());
                    start.Explore();
                }
            }
            Console.WriteLine("completed.");
        }

        static void CreatePathFile()
        {
            if((Program.PossiblePaths?.Count ?? 0) == 0)
                Program.ExploreAllPossiblePaths();

            Console.Write("Creatring 'paths' file ... ");
            using (var writer = new StreamWriter(File.Open(Program.CombinationFile, FileMode.Create)))
            {
                foreach(var item in Program.PossiblePaths)
                {
                    writer.WriteLine(string.Join(" ", item));
                }
            }

            Console.WriteLine("completed.");
        }

        static void ReadPathFile()
        {
            Program.PossiblePaths.Clear();

            Console.Write("Reading 'paths' file ... ");
            using (var reader = new StreamReader(File.Open(Program.CombinationFile, FileMode.Open, FileAccess.Read)))
            {
                var path = new List<int>();

                while(!reader.EndOfStream)
                {
                    path.Clear();

                    var line = reader.ReadLine();
                    var vals = line.Split(" ");

                    if (vals.Length >= MIN_LENGTH && vals.Length <= MAX_LENGTH)
                    {
                        foreach (var val in vals)
                            path.Add(int.Parse(val));

                        Program.PossiblePaths.Add(path.ToArray());
                    }
                }
            }

            Console.WriteLine("completed.");

        }
    }

    public class Node
    {
        private Stack<int> _path;
        private ValueTuple<int, int> _pos;
        
        private Node() { }

        public Node(ValueTuple<int, int> position, Stack<int> previousPath)
        {
            _pos = position;
            _path = previousPath;
            _path.Push(Program.PathMatrix[_pos.Item1][_pos.Item2]);
        }

        public void Explore()
        {
            foreach(var move in Program.Moves)
            {
                var newPosition = (_pos.Item1 + move.Item1, this._pos.Item2 + move.Item2);
                
                if((newPosition.Item1 > -1 && newPosition.Item1 < 4) && 
                   (newPosition.Item2 > -1 && newPosition.Item2 < 4) &&
                   _path.Contains(Program.PathMatrix[newPosition.Item1][newPosition.Item2]) == false)
                {
                    var subnode = new Node(newPosition, this._path);
                    subnode.Explore();
                }

                // get's here if the solutions can't move to the next node
            }

            var path = new List<int>(_path.ToArray());
            path.Reverse();

            // Add to final solution
            Program.PossiblePaths.Add(path.ToArray());

            // Remove currnent path before rolling back one step
            this._path.Pop();
        }
    }


}
