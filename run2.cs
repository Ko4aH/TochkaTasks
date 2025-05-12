using System;
using System.Collections.Generic;

using System.Linq;


class Program
{
    static readonly char[] KeysChar = Enumerable.Range('a', 26).Select(i => (char)i).ToArray();
    static readonly char[] DoorsChar = KeysChar.Select(char.ToUpper).ToArray();
    static readonly int[] DX = { 1, -1, 0, 0 };
    static readonly int[] DY = { 0, 0, 1, -1 };
    
    static HashSet<char> CharToCollect { get; set; } = new();

    class State : IEquatable<State>
    {
        public readonly int[] RobotPos;
        public readonly HashSet<char> Keys;
        private readonly int hash;

        public State(int[] robots, HashSet<char> keys)
        {
            RobotPos = robots.ToArray();
            Keys   = new HashSet<char>(keys);
            unchecked
            {
                hash = 17;
                foreach (var p in RobotPos) hash = hash * 31 + p;
                foreach (var k in Keys.OrderBy(c=>c)) hash = hash * 31 + k;
            }
        }

        public bool Equals(State other)
        {
            if (other is null) return false;
            if (!hash.Equals(other.hash)) return false;
            for (int i = 0; i < RobotPos.Length; i++)
                if (RobotPos[i] != other.RobotPos[i]) return false;
            return Keys.SetEquals(other.Keys);
        }
        public override bool Equals(object obj) => Equals(obj as State);
        public override int GetHashCode() => hash;
    }
    
    class StateComparer : IEqualityComparer<State>
    {
        public bool Equals(State x, State y) => x.Equals(y);
        public int GetHashCode(State obj) => obj.GetHashCode();
    }

    class Edge
    {
        public char From { get; set; }
        public char To { get; set; }
        public int Cost { get; set; }
        public HashSet<char> KeyNeeded { get; set; }
        public HashSet<char> KeyCollected { get; set; }

        public Edge(char from, char to, int cost, HashSet<char> keyCollected, HashSet<char> keyNeeded)
        {
            From = from;
            To = to;
            Cost = cost;
            KeyNeeded = keyNeeded;
            KeyCollected = keyCollected;
        }
    }

    class Vertice
    {
        public (int, int) Coordinates { get; set; }
        public char Char { get; set; }

        public Vertice((int, int) coordinates, char c)
        {
            Coordinates = coordinates;
            Char = c;
        }
    }

    class BfsResult
    {
        public Dictionary<(int, int), int> Distances { get; set; }
        public Dictionary<(int, int), HashSet<char>> Keys { get; set; }
        public Dictionary<(int, int), HashSet<char>> Doors { get; set; }

        public BfsResult(Dictionary<(int, int), int> distances, 
            Dictionary<(int, int), HashSet<char>> keys, Dictionary<(int, int), HashSet<char>> doors)
        {
            Distances = distances;
            Keys = keys;
            Doors = doors;
        }

        public bool HasPoi(Vertice poi)
        {
            return Distances.ContainsKey(poi.Coordinates);
        }
    }
    
    static List<List<char>> GetInput()
    {
        var data = new List<List<char>>();
        string line;

        while ((line = Console.ReadLine()) is not null)
        {
            if (line.Length == 0)
                break;
            data.Add(line.ToCharArray().ToList());
        }
        
        return data;
    }


    static int Solve(List<List<char>> data)
    {
        var pois = GetPois(data);
        var distanceTable = GetDistancesBetweenPois(data, pois);
        var result = Dijkstra(distanceTable, pois);
        
        return result;
    }

    static int Dijkstra(Edge[,] distanceTable, Vertice[] pois)
    {
        var pq = new PriorityQueue<State, int>();
        var robotPos = Enumerable.Range(0, 4).ToArray();
        var collectedKeys = new HashSet<char>();
        var initialState = new State(robotPos, collectedKeys);
        pq.Enqueue(initialState, 0);
        
        var best = new Dictionary<State, int>(new StateComparer());
        best[initialState] = 0;
        
        while (pq.Count > 0)
        {
            pq.TryDequeue(out var state, out int costSoFar);

            if (state.Keys.Count == CharToCollect.Count)
            {
                return costSoFar;
            }
            
            for (int robotId = 0; robotId < 4; robotId++)
            {
                var posIndex = state.RobotPos[robotId];
                var toBreak = false;
                
                for (int i = 0; i < distanceTable.GetLength(0); i++)
                {
                    var edge = distanceTable[posIndex, i];
                    if (edge == null)
                    {
                        continue;
                    }
                    var nextKey = edge.To;
                    if (state.Keys.Contains(nextKey)
                        || !edge.KeyNeeded.All(k => state.Keys.Contains(k)))   
                    {
                        continue;
                    }

                    var newRobotPos = state.RobotPos.ToArray();
                    newRobotPos[robotId] = i;
                    var newKeys = new HashSet<char>(state.Keys);
                    newKeys.Add(nextKey);
                    var newState = new State(newRobotPos, newKeys);
                    var newCost = costSoFar + edge.Cost;
                    
                    if (!best.TryGetValue(newState, out int oldCost) || newCost < oldCost)
                    {
                        best[newState] = newCost;
                        pq.Enqueue(newState, newCost);
                        break;
                    }
                }

                if (toBreak)
                {
                    break;
                }
            }
        }
        
        return -1;
    }

    static Edge[,] GetDistancesBetweenPois(List<List<char>> data, Vertice[] pois)
    {
        var distanceTable = new Edge[pois.Length, pois.Length];
        for (int i = 0; i < pois.Length; i++)
        {
            var bfsResult = BfsFrom(data, pois[i].Coordinates);
            for (int j = 0; j < pois.Length; j++)
            {
                if (i == j)
                {
                    continue;
                }

                if (bfsResult.HasPoi(pois[j]))
                {
                    distanceTable[i, j] = new Edge(
                        pois[i].Char, 
                        pois[j].Char, 
                        bfsResult.Distances[pois[j].Coordinates],
                        bfsResult.Keys[pois[j].Coordinates],
                        bfsResult.Doors[pois[j].Coordinates]);
                }
            }
        }

        return distanceTable;
    }

    static BfsResult BfsFrom(List<List<char>> data, (int, int) start)
    {
        var distances = new Dictionary<(int, int), int>();
        distances[start] = 0;
        
        var keys = new Dictionary<(int, int), HashSet<char>>();
        keys[start] = new HashSet<char>();
        
        var doors = new Dictionary<(int, int), HashSet<char>>();
        doors[start] = new HashSet<char>();
        
        var queue = new Queue<(int, int)>();
        queue.Enqueue(start);

        var rows = data.Count;
        var columns = data[0].Count;
        
        while (queue.Count > 0)
        {
            var currentPos = queue.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                var nextPos = (currentPos.Item1 + DX[i], currentPos.Item2 + DY[i]);
                var next = data[nextPos.Item2][nextPos.Item1];
                if (nextPos.Item1 < 0 
                    || nextPos.Item1 > rows 
                    || nextPos.Item2 < 0 
                    || nextPos.Item2 > columns 
                    || next == '#'
                    || distances.ContainsKey(nextPos))
                {
                    continue;
                }

                distances[nextPos] = distances[currentPos] + 1;
                
                keys[nextPos] = new HashSet<char>(keys[currentPos]);
                if (char.IsLower(next))
                    keys[nextPos].Add(next);
                
                doors[nextPos] = new HashSet<char>(doors[currentPos]);
                if (char.IsUpper(next))
                    doors[nextPos].Add(next);
                
                queue.Enqueue(nextPos);
            }
        }

        return new BfsResult(distances, keys, doors);
    }

    static Vertice[] GetPois(List<List<char>> data)
    {
        var pois = new List<Vertice>();
        for (int i = 0; i < data.Count; i++)
        {
            for (int j = 0; j < data[i].Count; j++)
            {
                var cell = data[i][j];
                if (cell == '@')
                {
                    pois.Add(new Vertice(
                        (j, i),
                        cell));
                }

                if (KeysChar.Contains(cell))
                {
                    pois.Add(new Vertice(
                        (j, i),
                        cell));
                    CharToCollect.Add(cell);
                }
            }
        }

        var sortedPois = pois
            .OrderBy(poi => poi.Char == '@' ? 0 : 1)
            .ThenBy(poi => poi.Char);
        return sortedPois.ToArray();
    }

    static void Main()
    {
        var data = GetInput();
        var result = Solve(data);
        
        if (result == -1)
        {
            Console.WriteLine("No solution found");
        }
        else
        {
            Console.WriteLine(result);
        }
    }
}