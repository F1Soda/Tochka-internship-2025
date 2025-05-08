using System;
using System.Collections.Generic;
using System.Linq;

public class BinaryHeap<T>
{
    private List<T> arr = new List<T>();
    private Comparer<T> comparer;
    public int Count => arr.Count;


    public BinaryHeap(Comparer<T> comparer)
    {
        this.comparer = comparer;
    }

    public void Add(T item)
    {
        arr.Add(item);
        HeapifyUp();
    }

    public T PopMin()
    {
        var res = arr[0];
        arr[0] = arr[Count - 1];
        HeapifyDown();
        arr.RemoveAt(arr.Count - 1);
        return res;
    }

    private void HeapifyDown()
    {
        var itemIndex = 0;
        while (true)
        {
            var childIndex1 = itemIndex * 2 + 1;
            var childIndex2 = itemIndex * 2 + 2;

            if (childIndex1 >= Count) break;

            var minInd = 0;
            if (childIndex2 >= Count)
            {
                minInd = childIndex1;
            }
            else
            {
                var minVal = comparer.Compare(arr[childIndex1], arr[childIndex2]) < 0
                    ? arr[childIndex1]
                    : arr[childIndex2];
                minInd = comparer.Compare(arr[childIndex1], minVal) == 0 ? childIndex1 : childIndex2;
            }

            if (comparer.Compare(arr[itemIndex], arr[minInd]) > 0)
            {
                (arr[itemIndex], arr[minInd]) = (arr[minInd], arr[itemIndex]);
                itemIndex = minInd;
            }
            else
                break;
        }
    }

    private void HeapifyUp()
    {
        var itemIndex = Count - 1;
        // while(itemIndex > 0 && arr[itemIndex] < arr[(itemIndex - 1) / 2])
        while (itemIndex > 0 && comparer.Compare(arr[itemIndex], arr[(itemIndex - 1) / 2]) < 0)
        {
            var parentIndex = (itemIndex - 1) / 2;
            (arr[itemIndex], arr[parentIndex]) = (arr[parentIndex], arr[itemIndex]);
            itemIndex = parentIndex;
        }
    }
}


class run2
{
    // Константы для символов ключей и дверей
    static readonly char[] keys_char = Enumerable.Range('a', 26).Select(i => (char)i).ToArray();
    static readonly char[] doors_char = keys_char.Select(char.ToUpper).ToArray();

    private static readonly List<(int dr, int dc)> moves = new List<(int dr, int dc)>()
        { (0, 1), (0, -1), (1, 0), (-1, 0) };

    // Метод для чтения входных данных
    static List<List<char>> GetInput()
    {
        var data = new List<List<char>>();
        string line;
        while ((line = Console.ReadLine()) != null && line != "")
        {
            data.Add(line.ToCharArray().ToList());
        }

        return data;
    }

    static int Encode4Char2Int(char a, char b, char c, char d)
    {
        return (a << 24) | (b << 16) | (c << 8) | d;
    }

    static char GetCharFromEncodedInt(int value, int index)
    {
        int shift = (3 - index) * 8;
        return (char)((value >> shift) & 0xFF);
    }

    static int SetCharInEncodedInt(int value, int index, char ch)
    {
        int shift = (3 - index) * 8;
        int mask = ~(0xFF << shift);
        int newChar = ch << shift;
        return (value & mask) | newChar;
    }

    static int Solve(List<List<char>> data)
    {
        // Идея решения:
        // На самом деле можно рассматривать перемещения роботов не на всем графе лабиринта, 
        // а лишь на подграфе G, где есть ключи, двери, стартовые позиции роботов.
        // Будем называть эти вершины `особенными`
        // 
        // Мы можем заранее расcчитать кротчайшие пути, чтобы указать веса переходов в графе G
        //
        // Важно!
        // Вершины u и v будут соеденены только в том случае, если
        // 1. Между ними есть путь
        // 2. Этот путь не содержит другие вершины графа G
        // Например:
        // #######
        // #b.a.@#
        // #######
        // Тогда смежные стороны выглядят следующим образом:
        // {
        //   @ : [(a, 2)] 
        //   a : [(b, 2), (@, 2)]
        //   b : [(a, 2)]
        // }
        //
        // Рассчитав кратчайшие пути от каждого с каждым в графе G запустим алгоритм Дейкстры(тот же самый BFS,
        // но только во взевшенном графе) до тех пор, пока не дойдем до момента, когда в первый раз соберём 
        // все ключи.
        //
        // Пусть k = количеству ключей в лабиринте, тогда
        // временная сложность:
        // O(`Run bfs on each special vertices` + `Run Dijkstra on graph G`) = O(nm(2k + 4) + ElogN),
        // где N = (2k + 4)^4 * 2^k, а E = N * (2k + 4)^4;


        var (n, m) = (data.Count, data[0].Count);

        // Ищем позиции особых вершин и начальные позиции роботов
        // ------------------------------------------------------

        // символу сопоставляем его позицию
        var specialVertices = new Dictionary<char, (int r, int c)>(4 + 26 * 2);

        // Координате сопоставляем номер робота, который стоит на ней.
        var starts = new Dictionary<(int r, int c), char>(4);

        var countKeys = 0;
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                if (data[i][j] == '@')
                {
                    // Нумеруем роботов от 0 до 3
                    var c = (char)('0' + starts.Count);
                    starts[(i, j)] = c;
                    specialVertices[c] = (i, j);
                }
                else if (data[i][j] != '#' && data[i][j] != '.')
                {
                    specialVertices[data[i][j]] = (i, j);
                    if ('a' <= data[i][j] && data[i][j] <= 'z')
                        countKeys++;
                }
            }
        }

        // Находим кротчайшие расстояния между особоыми вершинами в графе G
        // ----------------------------------------------------------------

        IEnumerable<(int r, int c)> GetNeighbors((int r, int c) pos)
        {
            foreach (var (dr, dc) in moves)
            {
                var (newR, newC) = (pos.r + dr, pos.c + dc);
                if (newR >= 0 && newR < n && newC >= 0 && newC < m && data[newR][newC] != '#')
                    yield return (newR, newC);
            }
        }

        var graph = new Dictionary<char, Dictionary<char, int>>(specialVertices.Count);

        // Для каждой specialVertex находим кротчайшее расстояние между всеми остальными в specialVertices. BFS
        foreach (var specialVertex in specialVertices.Keys)
        {
            graph[specialVertex] = new Dictionary<char, int>();
            var startPos = specialVertices[specialVertex];
            var visited = new HashSet<(int, int)>();
            var queue = new Queue<((int r, int c), int distance)>();

            queue.Enqueue((startPos, 0));
            visited.Add(startPos);

            while (queue.Count > 0)
            {
                var (pos, distance) = queue.Dequeue();
                var c = data[pos.r][pos.c];
                if (c != '.' && c != specialVertex)
                {
                    if (c == '@')
                        c = starts[pos];
                    if (c != specialVertex)
                    {
                        graph[specialVertex][c] = distance;
                        // Если мы встретили особый символ, то дальнейший путь останавливается.
                        continue;
                    }
                }

                foreach (var neighbor in GetNeighbors(pos))
                    if (!visited.Contains(neighbor))
                    {
                        queue.Enqueue((neighbor, distance + 1));
                        visited.Add(neighbor);
                    }
            }
        }

        // Запускаем Dijkstra в получившемся графе
        // ---------------------------------------
        var finalMask = (1 << countKeys) - 1;

        var heap = new BinaryHeap<(int dist, int robotsPos, int mask)>(
            Comparer<(int dist, int robotsPos, int mask)>.Create((a, b) => a.dist.CompareTo(b.dist))
        );
        
        // Каждый робот может находиться в одном из 2 * k + 4 позиций, где k - количество ключей.
        // Так как K <= 26, то каждую позицию можно задать с помощью однобайтового char
        // Всего роботов 4, значит понадобится 4 байта, что отлично вмещается в int.
        heap.Add((0, Encode4Char2Int('0', '1', '2', '3'), 0));

        var distances = new Dictionary<(int robotsPos, int mask), int>();
        distances[(Encode4Char2Int('0', '1', '2', '3'), 0)] = 0;

        while (heap.Count > 0)
        {
            var (distance, robotsPos, mask) = heap.PopMin();
            if (distances.ContainsKey((robotsPos, mask)) && distances[(robotsPos, mask)] < distance)
                continue;
            if (mask == finalMask)
                return distance;
            for (int i = 0; i < 4; i++)
            {
                var c = GetCharFromEncodedInt(robotsPos, i);
                foreach (var neighbor in graph[c])
                {
                    var destination = neighbor.Key;
                    var cost = neighbor.Value;

                    var mask2 = mask;
                    // Если встретили ключ. Обновляем маску
                    if ('a' <= destination && destination <= 'z')
                        mask2 |= 1 << (destination - 'a');
                    // Если встретили дверь. Проверяем что можем пройти
                    else if ('A' <= destination && destination <= 'Z')
                        if ((mask & (1 << (destination - 'a'))) == 0)
                            continue;

                    var newRobotsPos = SetCharInEncodedInt(robotsPos, i, destination);

                    if (distances.ContainsKey((newRobotsPos, mask2)) &&
                        distances[(newRobotsPos, mask2)] <= cost + distance)
                        continue;

                    distances[(newRobotsPos, mask2)] = cost + distance;
                    heap.Add((cost + distance, newRobotsPos, mask2));
                }
            }
        }

        return -1;
    }

    static void Main()
    {
        var data = GetInput();
        int result = Solve(data);

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