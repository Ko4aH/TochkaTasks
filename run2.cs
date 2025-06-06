﻿using System;
using System.Collections.Generic;
using System.Linq;


class Program
{
    static readonly char[] KeysChar = Enumerable.Range('a', 26).Select(i => (char)i).ToArray();
    static readonly char[] DoorsChar = KeysChar.Select(char.ToUpper).ToArray();
    static readonly int[] Dx = { 1, -1, 0, 0 };
    static readonly int[] Dy = { 0, 0, 1, -1 };

    static HashSet<char> CharToCollect { get; set; } = new HashSet<char>();
    private static int RobotCount { get; set; }

    class State : IEquatable<State>
    {
        public int[] RobotPos { get; }
        public uint KeysMask { get; }


        public State(int[] robotPos, uint keysMask)
        {
            RobotPos = robotPos;
            KeysMask = keysMask;
        }

        public bool HasKey(char k) => (KeysMask & (1u << (k - 'a'))) != 0;

        public int KeysCount
        {
            get
            {
                var mask = KeysMask;
                var count = 0;
                while (mask != 0)
                {
                    mask &= mask - 1;
                    count++;
                }

                return count;
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var h = (int)KeysMask;
                foreach (var p in RobotPos)
                {
                    h = h * 31 + p;
                }

                return h;
            }
        }

        public bool Equals(State other)
        {
            if (other is null || KeysMask != other.KeysMask)
            {
                return false;
            }

            for (int i = 0; i < RobotPos.Length; i++)
                if (RobotPos[i] != other.RobotPos[i])
                {
                    return false;
                }

            return true;
        }

        public override bool Equals(object obj) => obj is State s && Equals(s);
    }

    class StateComparer : IEqualityComparer<State>
    {
        public bool Equals(State x, State y) => x.Equals(y);
        public int GetHashCode(State obj) => obj.GetHashCode();
    }

    class Edge
    {
        public char From { get; set; }
        public char To { get; }
        public int Cost { get; }
        public HashSet<char> KeyNeeded { get; }
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
        public (int, int) Coordinates { get; }
        public char Char { get; }

        public Vertice((int, int) coordinates, char c)
        {
            Coordinates = coordinates;
            Char = c;
        }
    }

    class BfsResult
    {
        public Dictionary<(int, int), int> Distances { get; }
        public Dictionary<(int, int), HashSet<char>> Keys { get; }
        public Dictionary<(int, int), HashSet<char>> Doors { get; }

        public BfsResult(Dictionary<(int, int), int> distances,
            Dictionary<(int, int), HashSet<char>> keys, Dictionary<(int, int), HashSet<char>> doors)
        {
            Distances = distances;
            Keys = keys;
            Doors = doors;
        }

        public bool HasPoi((int, int) point)
        {
            return Distances.ContainsKey(point);
        }
    }

    static List<List<char>> GetInput()
    {
        var data = new List<List<char>>();
        string line;

        while ((line = Console.ReadLine()) != null)
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
        var result = Dijkstra(distanceTable);

        return result;
    }

    static int Dijkstra(Edge[,] distanceTable)
    {
        var pq = new SortedDictionary<int, Queue<State>>();

        void Enqueue(State state, int cost)
        {
            if (!pq.TryGetValue(cost, out var bucket))
                pq[cost] = bucket = new Queue<State>();
            bucket.Enqueue(state);
        }

        bool TryDequeue(out State state, out int cost)
        {
            if (pq.Count == 0)
            {
                state = null;
                cost = 0;
                return false;
            }

            var first = pq.First();
            cost = first.Key;
            var queue = first.Value;
            state = queue.Dequeue();
            if (queue.Count == 0) pq.Remove(first.Key);
            return true;
        }

        var robotPos = Enumerable.Range(0, RobotCount).ToArray();
        var initialState = new State(robotPos, 0);
        Enqueue(initialState, 0);

        var best = new Dictionary<State, int>(new StateComparer());
        best[initialState] = 0;

        while (TryDequeue(out var state, out int costSoFar))
        {
            if (state.KeysCount == CharToCollect.Count)
            {
                return costSoFar;
            }

            for (int robotId = 0; robotId < RobotCount; robotId++)
            {
                var posIndex = state.RobotPos[robotId];

                for (int i = 0; i < distanceTable.GetLength(0); i++)
                {
                    var edge = distanceTable[posIndex, i];
                    if (edge == null)
                    {
                        continue;
                    }

                    var nextKey = edge.To;
                    if (state.HasKey(nextKey)
                        || nextKey == '@'
                        || !edge.KeyNeeded.All(k => state.HasKey(char.ToLower(k))))
                    {
                        continue;
                    }

                    var newRobotPos = state.RobotPos.ToArray();
                    newRobotPos[robotId] = i;
                    var newKeys = state.KeysMask | (1u << (nextKey - 'a'));
                    var newState = new State(newRobotPos, newKeys);
                    var newCost = costSoFar + edge.Cost;

                    if (best.TryGetValue(newState, out int oldCost) && newCost >= oldCost)
                    {
                        continue;
                    }

                    best[newState] = newCost;
                    Enqueue(newState, newCost);
                }
            }
        }

        return -1;
    }

    static Edge[,] GetDistancesBetweenPois(List<List<char>> data, Vertice[] pois)
    {
        var distanceTable = new Edge[pois.Length, pois.Length];
        for (int from = 0; from < pois.Length; from++)
        {
            var bfsResult = BfsFrom(data, pois[from].Coordinates);
            for (int to = 0; to < pois.Length; to++)
            {
                if (from == to)
                {
                    continue;
                }

                if (bfsResult.HasPoi(pois[to].Coordinates))
                {
                    distanceTable[from, to] = new Edge(
                        pois[from].Char,
                        pois[to].Char,
                        bfsResult.Distances[pois[to].Coordinates],
                        bfsResult.Keys[pois[to].Coordinates],
                        bfsResult.Doors[pois[to].Coordinates]);
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

        var result = new BfsResult(distances, keys, doors);

        var queue = new Queue<(int, int)>();
        queue.Enqueue(start);

        var rows = data[0].Count;
        var columns = data.Count;

        while (queue.Count > 0)
        {
            var currentPos = queue.Dequeue();
            for (int i = 0; i < 4; i++)
            {
                var nextPos = (currentPos.Item1 + Dx[i], currentPos.Item2 + Dy[i]);
                if (nextPos.Item1 < 0
                    || nextPos.Item1 >= rows
                    || nextPos.Item2 < 0
                    || nextPos.Item2 >= columns
                    || result.HasPoi(nextPos))
                {
                    continue;
                }

                var next = data[nextPos.Item2][nextPos.Item1];
                if (next == '#')
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

        return result;
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
                    RobotCount++;
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

        return pois
            .OrderBy(poi => poi.Char == '@' ? 0 : 1)
            .ThenBy(poi => poi.Char)
            .ToArray();
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