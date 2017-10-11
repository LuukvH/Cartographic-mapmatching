using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace Matching_Planar_Maps
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Graph _resultGraph;
        private GridGraph _graph;
        private Path _path;

        private string currentFile = "";

        private bool _debug = false;

        private bool _allowSameEdge = true;

        private const float TOLERANCE = 0.000001f;

        private Polygon inputPolygon;

        private Polygon outputPolygon0;
        private Polygon outputPolygon1;
        private Polygon outputPolygon2;
        private Polygon outputPolygon3;
        private Polygon outputPolygon4;
        private Polygon outputPolygon5;
        private Polygon outputPolygon6;
        private Polygon outputPolygon7;
        private Polygon outputPolygon8;

        private List<List<int>> possiblePaths = new List<List<int>>();
        private Polygon xorPolygon;

        // Free space parameters
        private int steps = 80;
        private int _size = 80;

        public float maxDistance = 1;
        public float epsilon { get; set; } = 10;
        private Boolean initialized = false;

        private List<FreeSpaceStrip>[] _freeSpaceStrips;

        private Interval _result = null;

        private string outputfolder = "Experiment6";

        private int reusedEdges = 0;
        private int reusedVertices = 0;
        private int intersections = 0;
        public float scale = 1.0f;

        private List<int> indexPath;

        public MainWindow()
        {
            InitializeComponent();

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            //dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Tick += new EventHandler(possiblePathTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 50);
            //dispatcherTimer.Start();
        }

        private int size = 70;
        public void init()
        {
            //_graph = new GridGraph(8, 8, 80f);
            initialized = true;
            _graph = new GridGraph(size, 1f);
            scale = 500.0f / _graph.GridSize();
            scale *= 1.0f;

            string filename = "./Samples/Vietnam.ipe";

            IFileReader fileReader = new IPEReader();


            _deltaX = 0;
            _deltaY = 0;
            _current_position = 0;
            lbl_curpos.Content = String.Format("current position: {0}", _current_position);

            _path = fileReader.ReadFile(filename);

            FileInfo file = new FileInfo(filename);
            currentFile = System.IO.Path.GetFileNameWithoutExtension(file.Name);

            Normalize();
            Center();

            //CalculateResult();
        }

        public void CalculateResult()
        {
            OutputCanvas.Children.Clear();

            // Generate input polygon
            inputPolygon = new Polygon();
            foreach (Vertex v in _path.V)
            {
                inputPolygon.Points.Add(new Point(v.X, v.Y));
            }

            List<int> result = Calculation(_graph, _path);
            //epsilon = 0.66558f;
            //epsilon = 0.707f;
            //List<int> result = BuildIndexPath(MapMatching.Calculate(_graph, _path, epsilon, _allowSameEdge));

            bool[,] mapMatchingCellmap = BuildCellMap(BuildVertexPath(result));
            bool[,] cellmap = BuildCellMap(BuildVertexPath(result));

            /*
            while (DetectReuse(result))
            {
                cellmap = PushAndPull(result);
                result = RetraceCellMap(cellmap, result);
            }*/

            //result = SegmentCutout(result);


            DrawResult(result, Brushes.Blue);

            //DetectCanvas.Children.Clear();
            //DrawCellMap(mapMatchingCellmap, cellmap);
            DetectReuse(result);

            
            indexPath = result;
            Graph graph = new Path(result.Count);
            List<Vertex> vertexPath = BuildVertexPath(result);
            graph.V = vertexPath.ToArray();
            _resultGraph = graph;

            Calculation(graph, _path);
            
        }

        public List<int> Calculation(Graph graph, Path path)
        {

            if (!initialized)
                init();

            float minValue = 0;
            float maxValue = 1;
            float epsilon = 1;

            // Determine max and minvalue
            MapMatching mapMatching = new MapMatching();
            while (mapMatching.Calculate(graph, path, epsilon, _allowSameEdge) == null)
            {
                minValue = maxValue;
                maxValue *= 2;
                epsilon = maxValue;
            }

            // Find epsilon between min and max
            while (maxValue - minValue > TOLERANCE)
            {
                epsilon = (minValue + maxValue) / 2;
                if (mapMatching.Calculate(graph, path, epsilon, _allowSameEdge) != null)
                {
                    maxValue = epsilon;
                }
                else
                {
                    minValue = epsilon;
                }
            }

            epsilon = maxValue;
            this.epsilon = epsilon;
            _result = mapMatching.Calculate(graph, path, epsilon, _allowSameEdge);

            return BuildIndexPath(_result);
        }

        public List<int> BuildIndexPath(Interval result)
        {
            if (result == null)
                return null;

            List<int> indexPath = new List<int>();
            while (result != null)
            {
                indexPath.Add(result.GraphIndex);
                result = result.PathPointer;
            }
            indexPath.Reverse();
            return indexPath;
        }

        public List<Vertex> BuildVertexPath(List<int> indexPath)
        {
            List<Vertex> vertexPath = new List<Vertex>();
            for (int i = 0; i < indexPath.Count; i++)
            {
                vertexPath.Add(new Vertex(_graph.V[indexPath[i]].X, _graph.V[indexPath[i]].Y));
            }
            return vertexPath;
        }

        public bool[,] PushAndPull(List<int> indexPath)
        {
            // Build vertexpath
            List<Vertex> vertexPath = BuildVertexPath(indexPath);

            bool[,] cellmap = BuildCellMap(vertexPath);

            // Remove reused edges
            for (int i = 1; i < indexPath.Count; i++)
            {
                for (int n = i + 1; n < indexPath.Count; n++)
                {
                    if ((indexPath[i] == indexPath[n] && indexPath[i - 1] == indexPath[n - 1]) ||
                        (indexPath[i - 1] == indexPath[n] && indexPath[i] == indexPath[n - 1]))
                    {
                        GridGraph gg = (_graph as GridGraph);

                        int v1 = indexPath[i - 1];
                        int x1 = gg.GridX(v1);
                        int y1 = gg.GridY(v1);

                        int v2 = indexPath[i];
                        int x2 = gg.GridX(v2);
                        int y2 = gg.GridY(v2);

                        // Horizontal
                        Vertex c1 = null;
                        Vertex c2 = null;
                        if (_graph.V[v1].X.Equals(_graph.V[v2].X))
                        {
                            c1 = new Vertex(((_graph.V[v1].X + _graph.V[v2].X) / 2) + 0.5f,
                                ((_graph.V[v1].Y + _graph.V[v2].Y) / 2));
                            c2 = new Vertex(((_graph.V[v1].X + _graph.V[v2].X) / 2) - 0.5f,
                                ((_graph.V[v1].Y + _graph.V[v2].Y) / 2));
                        }
                        else if (_graph.V[v1].Y.Equals(_graph.V[v2].Y))
                        {
                            c1 = new Vertex(((_graph.V[v1].X + _graph.V[v2].X) / 2), ((_graph.V[v1].Y + _graph.V[v2].Y) / 2) - 0.5f);
                            c2 = new Vertex(((_graph.V[v1].X + _graph.V[v2].X) / 2), ((_graph.V[v1].Y + _graph.V[v2].Y) / 2) + 0.5f);
                        }

                        int count1 = SurroundingCount(c1, vertexPath);
                        int count2 = SurroundingCount(c2, vertexPath);

                        // If both are inside remove the vertex
                        if (IsPointInPolygon(c1, vertexPath) && IsPointInPolygon(c2, vertexPath))
                        {
                            if (count1 < count2)
                            {
                                cellmap[(int)Math.Floor(c1.X), (int)Math.Floor(c1.Y)] = false;
                                //DetectCanvas.Children.Add(DrawCircle(c1, 5, 4, Brushes.Red));
                                break;
                            }
                            else if (count2 < count1)
                            {
                                cellmap[(int)Math.Floor(c2.X), (int)Math.Floor(c2.Y)] = false;
                                //DetectCanvas.Children.Add(DrawCircle(c2, 5, 4, Brushes.Red));
                                break;
                            }
                            else
                            {
                                cellmap[(int)Math.Floor(c1.X), (int)Math.Floor(c1.Y)] = false;
                                //cellmap[(int)Math.Floor(c2.X), (int)Math.Floor(c2.Y)] = false;
                                //DetectCanvas.Children.Add(DrawCircle(c1, 5, 4, Brushes.Red));
                                //DetectCanvas.Children.Add(DrawCircle(c2, 5, 4, Brushes.Red));
                                break;
                            }
                        }
                        else
                        {
                            if (count1 > count2)
                            {
                                cellmap[(int)Math.Floor(c1.X), (int)Math.Floor(c1.Y)] = true;
                                break;
                            }
                            else if (count2 > count1)
                            {
                                cellmap[(int)Math.Floor(c2.X), (int)Math.Floor(c2.Y)] = true;
                                break;
                            }
                            else
                            {
                                cellmap[(int)Math.Floor(c1.X), (int)Math.Floor(c1.Y)] = true;
                                cellmap[(int)Math.Floor(c2.X), (int)Math.Floor(c2.Y)] = true;
                                break;
                            }
                        }
                    }
                }
            }

            // Remove reused vertices
            // Detect reuse of vertices
            var duplicates = indexPath.GroupBy(i => i)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToList();
            foreach (int v in duplicates)
            {
                Console.Out.WriteLine(v);

                int vi1 = _graph.GridIndex(_graph.GridX(v) - 1, _graph.GridY(v) - 1);
                int vi2 = _graph.GridIndex(_graph.GridX(v), _graph.GridY(v) - 1);
                int vi3 = _graph.GridIndex(_graph.GridX(v) - 1, _graph.GridY(v));
                int vi4 = _graph.GridIndex(_graph.GridX(v), _graph.GridY(v));

                Vertex v1 = _graph.V[vi1] + new Vertex(0.5f, 0.5f);
                Vertex v2 = _graph.V[vi2] + new Vertex(0.5f, 0.5f);
                Vertex v3 = _graph.V[vi3] + new Vertex(0.5f, 0.5f);
                Vertex v4 = _graph.V[vi4] + new Vertex(0.5f, 0.5f);

                if (indexPath.IndexOf(v) - 1 <= 0 || indexPath.IndexOf(v) + 1 >= indexPath.Count)
                    continue;

                int vp = indexPath[indexPath.IndexOf(v) - 1];
                int vn = indexPath[indexPath.IndexOf(v) + 1];

                int vgrid1 = _graph.GridIndex(_graph.GridX(v), _graph.GridY(v) - 1);
                int vgrid2 = _graph.GridIndex(_graph.GridX(v) + 1, _graph.GridY(v));
                int vgrid3 = _graph.GridIndex(_graph.GridX(v), _graph.GridY(v) + 1);
                int vgrid4 = _graph.GridIndex(_graph.GridX(v) - 1, _graph.GridY(v));

                Vertex vg1 = null, vg2 = null;
                Vertex vr1 = null, vr2 = null;

                if (cellmap[(int)Math.Floor(v1.X), (int)Math.Floor(v1.Y)] == false
                    &&
                    cellmap[(int)Math.Floor(v4.X), (int)Math.Floor(v4.Y)] == false
                    &&
                    cellmap[(int)Math.Floor(v2.X), (int)Math.Floor(v2.Y)] == true
                    &&
                    cellmap[(int)Math.Floor(v3.X), (int)Math.Floor(v3.Y)] == true)
                {
                    vg1 = v1;
                    vg2 = v4;
                    vr1 = v2;
                    vr2 = v3;

                    if (vp == vgrid1 && vn == vgrid2 || vp == vgrid2 && vn == vgrid1 || vp == vgrid4 && vn == vgrid3 ||
                        vp == vgrid3 && vn == vgrid4)
                    {
                        int count1 = SurroundingCount(vr1, vertexPath);
                        int count2 = SurroundingCount(vr2, vertexPath);

                        if (count1 <= count2)
                        {
                            cellmap[(int)Math.Floor(vr1.X), (int)Math.Floor(vr1.Y)] = false;
                        }
                        else
                        {
                            cellmap[(int)Math.Floor(vr2.X), (int)Math.Floor(vr2.Y)] = false;
                        }
                    }
                    else
                    {
                        int count1 = SurroundingCount(vg1, vertexPath);
                        int count2 = SurroundingCount(vg2, vertexPath);

                        if (count1 >= count2)
                        {
                            cellmap[(int)Math.Floor(vg1.X), (int)Math.Floor(vg1.Y)] = true;
                        }
                        else
                        {
                            cellmap[(int)Math.Floor(vg2.X), (int)Math.Floor(vg2.Y)] = true;
                        }
                    }
                }

                if (cellmap[(int)Math.Floor(v1.X), (int)Math.Floor(v1.Y)] == true
                    &&
                    cellmap[(int)Math.Floor(v4.X), (int)Math.Floor(v4.Y)] == true
                    &&
                    cellmap[(int)Math.Floor(v2.X), (int)Math.Floor(v2.Y)] == false
                    &&
                    cellmap[(int)Math.Floor(v3.X), (int)Math.Floor(v3.Y)] == false)
                {
                    vg1 = v2;
                    vg2 = v3;
                    vr1 = v1;
                    vr2 = v4;

                    if (vp == vgrid1 && vn == vgrid2 || vp == vgrid2 && vn == vgrid1 || vp == vgrid4 && vn == vgrid3 ||
                        vp == vgrid3 && vn == vgrid4)
                    {
                        int count1 = SurroundingCount(vg1, vertexPath);
                        int count2 = SurroundingCount(vg2, vertexPath);

                        if (count1 >= count2)
                        {
                            cellmap[(int)Math.Floor(vg1.X), (int)Math.Floor(vg1.Y)] = true;
                        }
                        else
                        {
                            cellmap[(int)Math.Floor(vg2.X), (int)Math.Floor(vg2.Y)] = true;
                        }
                    }
                    else
                    {
                        int count1 = SurroundingCount(vr1, vertexPath);
                        int count2 = SurroundingCount(vr2, vertexPath);

                        if (count1 <= count2)
                        {
                            cellmap[(int)Math.Floor(vr1.X), (int)Math.Floor(vr1.Y)] = false;
                        }
                        else
                        {
                            cellmap[(int)Math.Floor(vr2.X), (int)Math.Floor(vr2.Y)] = false;
                        }
                    }
                }
            }
            return cellmap;
        }


        public bool[,] BuildCellMap(List<Vertex> vertexPath)
        {
            // Build cell map
            bool[,] cellmap = new bool[_graph.GridSize(), _graph.GridSize()];
            for (int x = 0; x < _graph.GridSize(); x++)
            {
                for (int y = 0; y < _graph.GridSize(); y++)
                {
                    if (IsPointInPolygon(new Vertex(x + 0.5f, y + 0.5f), vertexPath))
                        cellmap[x, y] = true;
                }
            }
            return cellmap;
        }

        public void DrawCellMap(bool[,] cellmap, bool[,] cellmap2)
        {
            for (int x = 1; x < _graph.GridSize() - 1; x++)
            {
                for (int y = 1; y < _graph.GridSize() - 1; y++)
                {
                    if (cellmap[x, y] == true && cellmap2[x, y] == false)
                    {
                        DetectCanvas.Children.Add(DrawCell(new Vertex(x, y), Brushes.Red));
                    }
                    else if (cellmap[x, y] == false && cellmap2[x, y] == true)
                    {
                        DetectCanvas.Children.Add(DrawCell(new Vertex(x, y), Brushes.Green));
                    }
                }
            }
        }

        public
            List<int> RetraceCellMap(bool[,] cellmap, List<int> indexPath)
        {
            List<int> retracedIndexPath = new List<int>();
            retracedIndexPath.Add(indexPath[0]);
            retracedIndexPath.Add(indexPath[1]);

            int size = 0;
            while (size != retracedIndexPath.Count && retracedIndexPath[0] != retracedIndexPath[retracedIndexPath.Count - 1])
            {
                size = retracedIndexPath.Count;

                // Retrace output
                int xdir = _graph.GridX(retracedIndexPath[size - 1]) - _graph.GridX(retracedIndexPath[size - 2]);
                int ydir = _graph.GridY(retracedIndexPath[size - 1]) - _graph.GridY(retracedIndexPath[size - 2]);

                // Moving horizontal
                if (xdir != 0)
                {
                    if (_debug)
                        Console.Out.WriteLine($"Moving horizontal ({xdir},{ydir})");

                    if (xdir == -1 && (cellmap[_graph.GridX(retracedIndexPath[size - 1]) - 1, _graph.GridY(retracedIndexPath[size - 1]) - 1] ^
                        cellmap[_graph.GridX(retracedIndexPath[size - 1]) - 1, _graph.GridY(retracedIndexPath[size - 1])]))
                    {
                        // Moving left
                        if (_debug)
                            Console.Out.WriteLine("Move left");

                        retracedIndexPath.Add(_graph.GridIndex(_graph.GridX(retracedIndexPath[size - 1]) - 1,
                            _graph.GridY(retracedIndexPath[size - 1])));
                    }
                    else if (xdir == 1 && (cellmap[_graph.GridX(retracedIndexPath[size - 1]), _graph.GridY(retracedIndexPath[size - 1]) - 1] ^
                        cellmap[_graph.GridX(retracedIndexPath[size - 1]), _graph.GridY(retracedIndexPath[size - 1])]))
                    {
                        // Moving right
                        if (_debug)
                            Console.Out.WriteLine("Move right");

                        retracedIndexPath.Add(_graph.GridIndex(_graph.GridX(retracedIndexPath[size - 1]) + 1,
                            _graph.GridY(retracedIndexPath[size - 1])));
                    }
                    else
                    {
                        if (_debug)
                            Console.Out.WriteLine("Moving down or up");

                        // Move down
                        if (cellmap[_graph.GridX(retracedIndexPath[size - 1]) - 1, _graph.GridY(retracedIndexPath[size - 1])] ^
                            cellmap[_graph.GridX(retracedIndexPath[size - 1]), _graph.GridY(retracedIndexPath[size - 1])])
                        {
                            if (_debug)
                                Console.Out.WriteLine("Moving down");

                            retracedIndexPath.Add(_graph.GridIndex(_graph.GridX(retracedIndexPath[size - 1]),
                                _graph.GridY(retracedIndexPath[size - 1]) + 1));
                        }

                        // Move up
                        if (cellmap[_graph.GridX(retracedIndexPath[size - 1]) - 1, _graph.GridY(retracedIndexPath[size - 1]) - 1] ^
                            cellmap[_graph.GridX(retracedIndexPath[size - 1]), _graph.GridY(retracedIndexPath[size - 1]) - 1])
                        {
                            if (_debug)
                                Console.Out.WriteLine("Moving up");

                            retracedIndexPath.Add(_graph.GridIndex(_graph.GridX(retracedIndexPath[size - 1]),
                                _graph.GridY(retracedIndexPath[size - 1]) - 1));
                        }
                    }
                }

                // Moving vertical
                if (ydir != 0)
                {
                    if (_debug)
                        Console.Out.WriteLine($"Moving vertical ({xdir},{ydir})");

                    if (
                        ydir == 1 && (
                        cellmap[
                            _graph.GridX(retracedIndexPath[size - 1]) - 1,
                            _graph.GridY(retracedIndexPath[size - 1])] ^
                        cellmap[
                            _graph.GridX(retracedIndexPath[size - 1]),
                            _graph.GridY(retracedIndexPath[size - 1])]))
                    {
                        if (_debug)
                            Console.Out.WriteLine("Moving down");

                        retracedIndexPath.Add(_graph.GridIndex(_graph.GridX(retracedIndexPath[size - 1]),
                            _graph.GridY(retracedIndexPath[size - 1]) + 1));
                    }
                    else if (ydir == -1 && cellmap[
                          _graph.GridX(retracedIndexPath[size - 1]) - 1,
                          _graph.GridY(retracedIndexPath[size - 1]) - 1] ^
                      cellmap[
                          _graph.GridX(retracedIndexPath[size - 1]),
                          _graph.GridY(retracedIndexPath[size - 1]) - 1])
                    {
                        if (_debug)
                            Console.Out.WriteLine("Moving up");

                        retracedIndexPath.Add(_graph.GridIndex(_graph.GridX(retracedIndexPath[size - 1]),
                            _graph.GridY(retracedIndexPath[size - 1]) - 1));
                    }
                    else
                    {
                        if (_debug)
                            Console.Out.WriteLine("Moving left or right");

                        // Move left
                        if (cellmap[_graph.GridX(retracedIndexPath[size - 1]) - 1, _graph.GridY(retracedIndexPath[size - 1]) - 1] ^
                            cellmap[_graph.GridX(retracedIndexPath[size - 1]) - 1, _graph.GridY(retracedIndexPath[size - 1])])
                        {
                            if (_debug)
                                Console.Out.WriteLine("Moving left");

                            retracedIndexPath.Add(_graph.GridIndex(_graph.GridX(retracedIndexPath[size - 1]) - 1,
                                _graph.GridY(retracedIndexPath[size - 1])));
                        }

                        // Move right
                        if (cellmap[_graph.GridX(retracedIndexPath[size - 1]), _graph.GridY(retracedIndexPath[size - 1]) - 1] ^
                            cellmap[_graph.GridX(retracedIndexPath[size - 1]), _graph.GridY(retracedIndexPath[size - 1])])
                        {
                            if (_debug)
                                Console.Out.WriteLine("Moving right");

                            retracedIndexPath.Add(_graph.GridIndex(_graph.GridX(retracedIndexPath[size - 1]) + 1,
                                _graph.GridY(retracedIndexPath[size - 1])));
                        }
                    }
                }
            }
            retracedIndexPath.RemoveAt(retracedIndexPath.Count - 1);

            return retracedIndexPath;
        }

        public List<int> SegmentCutout(List<int> indexPath)
        {
            // Build vertexpath
            List<Vertex> vertexPath = BuildVertexPath(indexPath);
            List<int> resultPath = new List<int>();
            resultPath.AddRange(indexPath);

            GridGraph gg = (_graph as GridGraph);

            List<int> pointsOfInterests = new List<int>();
            // Remove reused edges
            for (int i = 1; i < indexPath.Count; i++)
            {
                for (int n = i + 1; n < indexPath.Count; n++)
                {
                    if ((indexPath[i] == indexPath[n] && indexPath[i - 1] == indexPath[n - 1]) ||
                        (indexPath[i - 1] == indexPath[n] && indexPath[i] == indexPath[n - 1]))
                    {
                        pointsOfInterests.Add(indexPath[i]);
                        pointsOfInterests.Add(indexPath[i - 1]);
                    }
                }
            }

            // Remove reused vertices
            // Detect reuse of vertices
            var duplicates = indexPath.GroupBy(i => i)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToList();
            foreach (int v in duplicates)
            {
                if (v != indexPath.Last())
                {
                    pointsOfInterests.Add(v);
                }
            }

            // DrawPointsOfInterest
            pointsOfInterests = pointsOfInterests.Distinct().ToList();
            foreach (int i in pointsOfInterests)
            {
                Ellipse ellipse = DrawCircle(_graph.V[i], 5, 2, Brushes.Green);
                DebugCanvas.Children.Add(ellipse);
            }

            // Find neighbor points of interest
            List<PartialProblem> partialProblems = new List<PartialProblem>();
            while (pointsOfInterests.Any())
            {
                PartialProblem pp = new PartialProblem(_graph);
                partialProblems.Add(pp);

                int point = pointsOfInterests.First();
                pp.addVertex(point);
                pointsOfInterests.Remove(point);

                bool anyInside = true;
                while (anyInside)
                {
                    anyInside = false;
                    foreach (int n in pointsOfInterests)
                    {
                        Vertex vf = _graph.V[n];
                        if (pp.isInside(vf))
                        {
                            pp.addVertex(n);

                            anyInside = true;
                        }
                    }

                    pointsOfInterests = pointsOfInterests.Except(pp.vertices).ToList();
                }
            }

            for (int n = 0; n < partialProblems.Count; n++)
            {
                
                //if (n != 1)
                //    continue;

               PartialProblem partialProblem = partialProblems[n];
                DrawBox(partialProblem.lbox, partialProblem.rbox, partialProblem.tbox, partialProblem.bbox, Brushes.Green);
                Path path = partialProblem.getPath(_path);
                Draw(DebugCanvas, path, Brushes.Red);

                foreach (int i in partialProblem.vertices)
                {
                    DebugCanvas.Children.Add(DrawCircle(_graph.V[i], 5, 2, Brushes.Green));
                }
                Graph partialGraph = partialProblem.partialGraph();

                //Draw(DebugCanvas, partialProblem.partialGraph(_graph), Brushes.Yellow);

                // Find in and out on indexpath
                int index = 0;
                int start = -1;
                int startIndex = -1;
                int endIndex = -1;
                int end = -1;
                while (!partialProblem.isInside(_graph.V[resultPath[index]]) || GraphFunctions.Distance(_graph.V[resultPath[index]], path.V[0]) > 0.8)
                {
                    index++;
                }
                start = resultPath[index];
                startIndex = index;

                while ((partialProblem.isInside(_graph.V[resultPath[index]]) || resultPath.GetRange(startIndex, index - startIndex).Intersect(partialProblem.vertices).Count() !=
                partialProblem.vertices.Count))
                {
                 //float dist = GraphFunctions.Distance(_graph.V[resultPath[index]], path.V[path.Size - 1]);
                    //Console.Out.WriteLine(dist);

                    index++;
                }

                endIndex = index - 1;
                end = resultPath[index - 1];
                

                DetectCanvas.Children.Add(DrawCircle(_graph.V[start], 5, 4, Brushes.Green));
                DetectCanvas.Children.Add(DrawCircle(_graph.V[end], 5, 4, Brushes.Red));

                continue;

                possiblePaths = partialProblem.getPossiblePaths(start, end);

                FrechetDistance frechetDistance = new FrechetDistance(partialGraph, path);
                List<List<int>> feasiblePaths = new List<List<int>>();

                float minValue = 0;
                float maxValue = 1;
                float epsilon = 1;

                

                // Determine max and minvalue
                while (!feasiblePaths.Any())
                {
                    Console.WriteLine($"{epsilon} - {possiblePaths.Count}");
                    feasiblePaths.Clear();

                    // Preprocess for all
                    frechetDistance.Preprocessing(epsilon);

                    for (int i = 0; i < possiblePaths.Count; i++)
                    {
                        if (i % 100000 == 0)
                            Console.WriteLine($"{i}/{possiblePaths.Count}");

                        if (frechetDistance.FeasiblePath(possiblePaths[i]))
                        {
                            feasiblePaths.Add(possiblePaths[i]);
                        }
                    }

                    Console.Out.WriteLine($"{possiblePaths.Count} : {feasiblePaths.Count}");

                    if (feasiblePaths.Any())
                    {
                        possiblePaths.Clear();
                        possiblePaths.AddRange(feasiblePaths);
                    }
                    else
                    {
                        minValue = maxValue;
                        maxValue *= 2;
                        epsilon = maxValue;
                        Console.WriteLine();
                    }
                }

                // Find epsilon between min and max
                while (maxValue - minValue > TOLERANCE)
                {
                    epsilon = (minValue + maxValue)/2;

                    feasiblePaths.Clear();

                    // Preprocess for all
                    frechetDistance.Preprocessing(epsilon);

                    for (int i = 0; i < possiblePaths.Count; i++)
                    {
                        if (i % 100000 == 0)
                            Console.WriteLine($"{i}/{possiblePaths.Count}");

                        if (frechetDistance.FeasiblePath(possiblePaths[i]))
                        {
                            feasiblePaths.Add(possiblePaths[i]);
                        }
                    }

                    Console.WriteLine($"{epsilon} - {possiblePaths.Count}");

                    if (feasiblePaths.Any())
                    {
                        maxValue = epsilon;
                        possiblePaths.Clear();
                        possiblePaths.AddRange(feasiblePaths);
                    }
                    else
                    {
                        minValue = epsilon;
                    }
                }


                // Edit graph
                frechetDistance.Preprocessing(maxValue);
                resultPath.RemoveRange(startIndex, (endIndex - startIndex) + 1);
                resultPath.InsertRange(startIndex, possiblePaths.First());

                for (int p = 0; p < Math.Min(5, possiblePaths.Count); p++)
                {
                    List<FreeSpaceStrip> freeSpaceStrips = frechetDistance.GenerateFreeSpaceStrips(possiblePaths[p], 80);
                    FreeSpaceDiagram freeSpaceDiagram = new FreeSpaceDiagram();
                    for (int s = 0; s < freeSpaceStrips.Count; s++)
                        {
                            freeSpaceDiagram.freeSpaceStack.Children.Insert(0, freeSpaceStrips[s]);
                        }
                    freeSpaceDiagram.Show();
                    freeSpaceDiagram.WindowState = WindowState.Maximized;
                }
                
                
            }

           return resultPath;
        }

        public Graph CreateGraphFromEdges(Graph graph, List<int> vertices)
        {
            Graph vertexGraph = new Graph(graph.Size);
            vertexGraph.V = graph.V;

            for (int i = 1; i < vertices.Count; i++)
            {
                vertexGraph.E[vertices[i-1]].Add(vertices[i]);
            }

            return vertexGraph;
        }

        public void DrawBox(float lbox, float rbox, float tbox, float bbox, Brush brush)
        {
            Line tline = DrawLine(new Vertex(lbox, tbox), new Vertex(rbox, tbox), brush);
            Line bline = DrawLine(new Vertex(lbox, bbox), new Vertex(rbox, bbox), brush);
            Line lline = DrawLine(new Vertex(lbox, tbox), new Vertex(lbox, bbox), brush);
            Line rline = DrawLine(new Vertex(rbox, tbox), new Vertex(rbox, bbox), brush);

            DetectCanvas.Children.Add(tline);
            DetectCanvas.Children.Add(bline);
            DetectCanvas.Children.Add(lline);
            DetectCanvas.Children.Add(rline);
        }

        public bool DetectReuse(List<int> indexPath)
        {
            // Reset counts
            reusedEdges = 0;
            reusedVertices = 0;
            intersections = 0;

            List<Vertex> vertexPath = BuildVertexPath(indexPath);

            // Detect edge reuse
            for (int i = 1; i < indexPath.Count; i++)
            {
                for (int n = i + 1; n < indexPath.Count; n++)
                {
                    if ((indexPath[i] == indexPath[n] && indexPath[i - 1] == indexPath[n - 1]) ||
                        (indexPath[i - 1] == indexPath[n] && indexPath[i] == indexPath[n - 1]))
                    {

                        reusedEdges++;

                        int v1 = indexPath[i - 1];
                        int v2 = indexPath[i];

                        Line line = DrawLine(_graph.V[v1], _graph.V[v2], Brushes.Red);
                        DetectCanvas.Children.Add(line);
                    }
                }
            }

            // Build cell map
            bool[,] cellmap = new bool[_graph.GridSize(), _graph.GridSize()];
            for (int x = 0; x < _graph.GridSize(); x++)
            {
                for (int y = 0; y < _graph.GridSize(); y++)
                {
                    if (IsPointInPolygon(new Vertex(x + 0.5f, y + 0.5f), vertexPath))
                        cellmap[x, y] = true;
                }
            }

            // Detect reuse of vertices
            var duplicates = indexPath.GroupBy(i => i)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToList();
            foreach (int v in duplicates)
            {
                int vi1 = _graph.GridIndex(_graph.GridX(v) - 1, _graph.GridY(v) - 1);
                int vi2 = _graph.GridIndex(_graph.GridX(v), _graph.GridY(v) - 1);
                int vi3 = _graph.GridIndex(_graph.GridX(v) - 1, _graph.GridY(v));
                int vi4 = _graph.GridIndex(_graph.GridX(v), _graph.GridY(v));

                Vertex v1 = _graph.V[vi1] + new Vertex(0.5f, 0.5f);
                Vertex v2 = _graph.V[vi2] + new Vertex(0.5f, 0.5f);
                Vertex v3 = _graph.V[vi3] + new Vertex(0.5f, 0.5f);
                Vertex v4 = _graph.V[vi4] + new Vertex(0.5f, 0.5f);

                if (v != indexPath.Last())
                {
                    reusedVertices++;
                    DetectCanvas.Children.Add(DrawCircle(_graph.V[v], 0.3f * scale, 2, Brushes.Red));
                }

                if (indexPath.IndexOf(v) - 1 <= 0 || indexPath.IndexOf(v) + 1 >= indexPath.Count)
                    continue;

                int vp = indexPath[indexPath.IndexOf(v) - 1];
                int vn = indexPath[indexPath.IndexOf(v) + 1];

                int vgrid1 = _graph.GridIndex(_graph.GridX(v), _graph.GridY(v) - 1);
                int vgrid2 = _graph.GridIndex(_graph.GridX(v) + 1, _graph.GridY(v));
                int vgrid3 = _graph.GridIndex(_graph.GridX(v), _graph.GridY(v) + 1);
                int vgrid4 = _graph.GridIndex(_graph.GridX(v) - 1, _graph.GridY(v));

                Vertex vg1 = null, vg2 = null;
                Vertex vr1 = null, vr2 = null;

                if ((cellmap[(int)Math.Floor(v1.X), (int)Math.Floor(v1.Y)] == false
                    &&
                    cellmap[(int)Math.Floor(v4.X), (int)Math.Floor(v4.Y)] == false
                &&
                    cellmap[(int)Math.Floor(v2.X), (int)Math.Floor(v2.Y)] == true
                    &&
                    cellmap[(int)Math.Floor(v3.X), (int)Math.Floor(v3.Y)] == true) || (cellmap[(int)Math.Floor(v1.X), (int)Math.Floor(v1.Y)] == true
                    &&
                    cellmap[(int)Math.Floor(v4.X), (int)Math.Floor(v4.Y)] == true
                    &&
                    cellmap[(int)Math.Floor(v2.X), (int)Math.Floor(v2.Y)] == false
                    &&
                    cellmap[(int)Math.Floor(v3.X), (int)Math.Floor(v3.Y)] == false))
                {

                    if (vp == vgrid1 && vn == vgrid3 || vp == vgrid3 && vn == vgrid1 || vp == vgrid4 && vn == vgrid2 ||
vp == vgrid2 && vn == vgrid4)
                    {

                        Line line = DrawLine(_graph.V[v] - new Vertex(0.5f, 0.5f), _graph.V[v] + new Vertex(0.5f, 0.5f), Brushes.Red);
                        Line line2 = DrawLine(_graph.V[v] + new Vertex(-0.5f, 0.5f), _graph.V[v] + new Vertex(0.5f, -0.5f), Brushes.Red);
                        DetectCanvas.Children.Add(line);
                        DetectCanvas.Children.Add(line2);

                        //DetectCanvas.Children.Add(DrawCircle(_graph.V[v], 12.0f, 5, Brushes.Red));
                        intersections++;
                    }
                }
            }

            if (reusedVertices + reusedEdges + intersections > 0)
                return true;

            return false;
        }

        public int SurroundingCount(Vertex c, List<Vertex> vertexPath)
        {

            // Get number of surounding cells inside
            int count = 0;
            count = IsPointInPolygon(new Vertex(c.X - 0.5f, c.Y - 0.5f), vertexPath) ? count + 1 : count;
            count = IsPointInPolygon(new Vertex(c.X, c.Y - 0.5f), vertexPath) ? count + 1 : count;
            count = IsPointInPolygon(new Vertex(c.X + 0.5f, c.Y - 0.5f), vertexPath) ? count + 1 : count;
            count = IsPointInPolygon(new Vertex(c.X - 0.5f, c.Y), vertexPath) ? count + 1 : count;
            count = IsPointInPolygon(new Vertex(c.X + 0.5f, c.Y), vertexPath) ? count + 1 : count;
            count = IsPointInPolygon(new Vertex(c.X - 0.5f, c.Y + 0.5f), vertexPath) ? count + 1 : count;
            count = IsPointInPolygon(new Vertex(c.X, c.Y + 0.5f), vertexPath) ? count + 1 : count;
            count = IsPointInPolygon(new Vertex(c.X - 0.5f, c.Y + 0.5f), vertexPath) ? count + 1 : count;
            return count;
        }

        public bool IsPointInPolygon(Vertex point, List<Vertex> polygon)
        {
            int polygonLength = polygon.Count, i = 0;
            bool inside = false;
            // x, y for tested point.
            float pointX = point.X, pointY = point.Y;
            // start / end point for the current polygon segment.
            float startX, startY, endX, endY;
            Vertex endPoint = polygon[polygonLength - 1];
            endX = endPoint.X;
            endY = endPoint.Y;
            while (i < polygonLength)
            {
                startX = endX; startY = endY;
                endPoint = polygon[i++];
                endX = endPoint.X; endY = endPoint.Y;
                //
                inside ^= (endY > pointY ^ startY > pointY) /* ? pointY inside [startY;endY] segment ? */
                          && /* if so, test if it is under the segment */
                          ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
            }
            return inside;
        }

        public List<Line> resultPath = new List<Line>();

        public void DrawQ(List<Interval> Q)
        {
            foreach (Interval q in Q)
            {
                Ellipse circle = CreateCircle(new Vertex(q.Start, 0), 20);
                circle.Fill = Brushes.Brown;
            }
        }


        private int currentindex = 0;
        private Line lineanimater = null;
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (lineanimater != null)
            {
                DetectCanvas.Children.Remove(lineanimater);
            }

            if (indexPath != null && indexPath.Count > 0)
            {
                currentindex = (currentindex + 1) % (indexPath.Count - 1);
                int nextindex = (currentindex + 1) % (indexPath.Count - 1);
                lineanimater = DrawLine(_graph.V[indexPath[currentindex]], _graph.V[indexPath[nextindex]],
                    Brushes.Red);
                DetectCanvas.Children.Add(lineanimater);
            }
        }

        private void possiblePathTimer_Tick(object sender, EventArgs e)
        {
            if (!possiblePaths.Any())
                return;

            OutputCanvas.Children.Clear();
            currentindex = (currentindex + 1) % (possiblePaths.Count - 1);
            OutputCanvas.Children.Clear();

            List<int> indexPath = possiblePaths[currentindex];
            for (int i = 1; i < indexPath.Count; i++)
            {
                Line line = DrawLine(_graph.V[indexPath[i - 1]], _graph.V[indexPath[i]], Brushes.Blue);
                OutputCanvas.Children.Add(line);
            }
        }

        #region Draw

        public void ReDraw()
        {
            ReDraw(false);
        }

        public void ReDraw(bool drawFreespace)
        {

            GridCanvas.Children.Clear();
            Draw(GridCanvas, _graph, Brushes.LightGray);
            Draw(InputCanvas, _path, Brushes.Black);

            //GenerateFreeSpaceStrips();

            for (int i = 0; i < _graph.Size; i++)
            {
                /*
                // For every outgoing edge
                for (int j = 0; j < _graph.E[i].Count; j++)
                {
                    FreeSpaceStrip freeSpaceStrip = _freeSpaceStrips[i][j];
                    
                    if (!freeSpaceStrip.active)
                        continue;

                    //if (drawFreespace)
                        DrawFreeSpaceDiagram(freeSpaceStrip);

                    DrawIntervals(freeSpaceStrip);

                    DrawLeftPointers(freeSpaceStrip);

                    DrawRightPointers(freeSpaceStrip);
                    
                }
                */
            }

            //DrawResult(_result);


        }

        public float Inv(float val)
        {
            return (val - _size) * -1;
        }

        public void Draw(Canvas canvas, Graph graph, Brush brush)
        {
            if (graph == null)
                return;

            for (int i = 0; i < graph.Size; i++)
            {
                List<int> edges = graph.E[i];
                foreach (int j in edges)
                {
                    Line line = new Line()
                    {
                        X1 = graph.V[i].X * scale,
                        Y1 = graph.V[i].Y * scale,
                        X2 = graph.V[j].X * scale,
                        Y2 = graph.V[j].Y * scale,
                        Stroke = brush,
                        StrokeThickness = 1
                    };
                    canvas.Children.Add(line);
                }
            }
        }

        public Line DrawLine(Vertex v1, Vertex v2, Brush brush)
        {
            Line line = new Line()
            {
                X1 = v1.X * scale,
                Y1 = v1.Y * scale,
                X2 = v2.X * scale,
                Y2 = v2.Y * scale
            };
            line.Stroke = brush;
            line.StrokeThickness = 2;
            return line;
        }

        public Ellipse CreateCircle(Vertex v, float r)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Width = 2 * r;
            ellipse.Height = 2 * r;
            Canvas.SetLeft(ellipse, v.X - r);
            Canvas.SetTop(ellipse, v.Y - r);

            return ellipse;
        }

        public Ellipse DrawCircle(Vertex v, float r, double StrokeThickness, Brush brush)
        {
            Ellipse ellipse = CreateCircle(new Vertex(v.X * scale, v.Y * scale), r);
            ellipse.Stroke = brush;
            ellipse.StrokeThickness = StrokeThickness;
            return ellipse;
        }

        public Polygon DrawCell(Vertex v, Brush brush)
        {
            Vertex v1 = new Vertex((float)Math.Floor(v.X), (float)Math.Floor(v.Y));
            Vertex v2 = v1 + new Vertex(1, 0);
            Vertex v3 = v1 + new Vertex(1, 1);
            Vertex v4 = v1 + new Vertex(0, 1);

            List<Vertex> vertices = new List<Vertex>();
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);
            vertices.Add(v4);

            Polygon polygon = DrawPolygon(vertices, brush);
            polygon.Opacity = 0.35;
            return polygon;
        }

        public Polygon DrawPolygon(List<Vertex> vertices, Brush brush)
        {
            Polygon polygon = new Polygon();
            foreach (Vertex vertex in vertices)
            {
                polygon.Points.Add(new Point(vertex.X, vertex.Y));
            }
            polygon.Fill = brush;
            polygon.LayoutTransform = new ScaleTransform(scale, scale, 0, 0);
            return polygon;
        }

        public void DrawResult(Canvas canvas, List<int> indexPath, Brush brush)
        {
            for (int i = 1; i < indexPath.Count; i++)
            {
                Line line = DrawLine(_graph.V[indexPath[i - 1]], _graph.V[indexPath[i]], brush);
                canvas.Children.Add(line);
            }
        }

        public void DrawResult(List<int> indexPath, Brush brush)
        {
            OutputCanvas.Children.Clear();
            RenderCanvas.Children.Clear();

            for (int i = 1; i < indexPath.Count; i++)
            {
                Line line = DrawLine(_graph.V[indexPath[i - 1]], _graph.V[indexPath[i]], brush);
                OutputCanvas.Children.Add(line);
            }

            List<Vertex> resultGraph = BuildVertexPath(indexPath);
            outputPolygon0 = new Polygon();
            outputPolygon1 = new Polygon();
            outputPolygon2 = new Polygon();
            outputPolygon3 = new Polygon();
            outputPolygon4 = new Polygon();
            outputPolygon5 = new Polygon();
            outputPolygon6 = new Polygon();
            outputPolygon7 = new Polygon();
            outputPolygon8 = new Polygon();
            foreach (Vertex v in resultGraph)
            {
                outputPolygon0.Points.Add(new Point(v.X - 1, v.Y - 1));
                outputPolygon1.Points.Add(new Point(v.X - 1, v.Y));
                outputPolygon2.Points.Add(new Point(v.X - 1, v.Y + 1));
                outputPolygon3.Points.Add(new Point(v.X, v.Y - 1));
                outputPolygon4.Points.Add(new Point(v.X, v.Y));
                outputPolygon5.Points.Add(new Point(v.X, v.Y + 1));
                outputPolygon6.Points.Add(new Point(v.X + 1, v.Y - 1));
                outputPolygon7.Points.Add(new Point(v.X + 1, v.Y));
                outputPolygon8.Points.Add(new Point(v.X + 1, v.Y + 1));
            }
            outputPolygon4.Fill = Brushes.Blue;
            outputPolygon4.Opacity = 0.25;
            outputPolygon4.LayoutTransform = new ScaleTransform(scale, scale, 0, 0);
            OutputPolygonCanvas.Children.Add(outputPolygon4);

            //outputPolygon0.Fill = Brushes.Purple;
            //outputPolygon0.Opacity = 0.25;
            //outputPolygon0.LayoutTransform = new ScaleTransform(scale, scale, 0, 0);
            //OutputPolygonCanvas.Children.Add(outputPolygon0);


            inputPolygon.Stroke = Brushes.Black;
            RenderCanvas.Children.Clear();
            RenderCanvas.Children.Add(inputPolygon);

            RenderCanvas.Children.Add(outputPolygon0);
            RenderCanvas.Children.Add(outputPolygon1);
            RenderCanvas.Children.Add(outputPolygon2);
            RenderCanvas.Children.Add(outputPolygon3);
            RenderCanvas.Children.Add(outputPolygon5);
            RenderCanvas.Children.Add(outputPolygon6);
            RenderCanvas.Children.Add(outputPolygon7);
            RenderCanvas.Children.Add(outputPolygon8);


        }

        #endregion

        #region Events

        private void canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            Point p = Mouse.GetPosition(GridCanvas);

            Vertex v = new Vertex((float)p.X / scale, (float)p.Y / scale);

            if (_path == null)
                _path = new Path(0);

            List<Vertex> V = _path.V.ToList();
            V.Add(v);


            _path = new Path(V.Count);
            _path.V = V.ToArray();

            // Generate input polygon
            inputPolygon = new Polygon();
            foreach (Vertex vertex in _path.V)
            {
                inputPolygon.Points.Add(new Point(vertex.X, vertex.Y));
            }

            InputCanvas.Children.Clear();
            Draw(InputCanvas, _path, Brushes.Black);

            if (_path.Size > 1)
            {

                CalculateResult();
            }

            //Calculation();

            //if (_result != null)
            //{
            //    lblEpsilon.Content = epsilon;
            //}
            //else
            //{
            //    lblEpsilon.Content = '-';
            //}

            //ReDraw(true);
        }


        private Ellipse circleSelector = null;
        private void canvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

            /*

            Point p = Mouse.GetPosition(DetectCanvas);
            Vertex c = new Vertex((float)p.X / scale, (float)p.Y / scale);

            if (circleSelector != null)
            {
                DetectCanvas.Children.Remove(circleSelector);
            }

            circleSelector = DrawCircle(c, epsilon * scale, 2, Brushes.Aquamarine);
            DetectCanvas.Children.Add(circleSelector);

            Vertex intersection1;
            Vertex intersection2;

            Graph graph = _resultGraph;

            for (int v = 0; v < graph.Size; v++)
            {
                for (int i = 0; i < graph.E[v].Count; i++)
                {
                    Vertex v1 = graph.V[v];
                    Vertex v2 = graph.V[graph.E[v][i]];

                    int nrOfIntersections = GraphFunctions.LineCircleIntersections(c, epsilon, v1, v2, out intersection1, out intersection2);

                    //if (nrOfIntersections == 0)
                    //    DetectCanvas.Children.Remove(intersectionLine);

                    if (nrOfIntersections == 2)
                    {
                        if (GraphFunctions.DistanceSquared(v1, c) < Math.Pow(epsilon, 2))
                        {
                            intersection2.X = v1.X;
                            intersection2.Y = v1.Y;
                        }

                        if (GraphFunctions.DistanceSquared(v2, c) < Math.Pow(epsilon, 2))
                        {
                            intersection1.X = v2.X;
                            intersection1.Y = v2.Y;
                        }

                        DetectCanvas.Children.Add(DrawLine(intersection1, intersection2, Brushes.Orange));
                    }

                    
                    //DrawIntersectionLine(intersection1, intersection2, Brushes.Yellow)
                }
            }
            */


            if (_path.V.Length > 0)
            {
                List<Vertex> V = _path.V.ToList();
                V.RemoveAt(V.Count - 1);
                _path = new Path(V.Count);
                _path.V = V.ToArray();

                // Generate input polygon
                inputPolygon = new Polygon();
                foreach (Vertex vertex in _path.V)
                {
                    inputPolygon.Points.Add(new Point(vertex.X, vertex.Y));
                }

                InputCanvas.Children.Clear();
                Draw(InputCanvas, _path, Brushes.Black);

                CalculateResult();
            }

            //if (_result != null)
            //{
            //    lblEpsilon.Content = epsilon;
            //}
            //else
            //{
            //    lblEpsilon.Content = '-';
            //}

            //ReDraw(true);

            //InputCanvas.Children.Clear();
            //Draw(InputCanvas, _path, Brushes.Black);
        }

        private Line _edge_highlighting = null;
        private void freespacestrip_MouseEnter(object sender, RoutedEventArgs e)
        {
            FreeSpaceStrip fss = ((FreeSpaceStrip)sender);

            try
            {
                OutputCanvas.Children.Remove(_edge_highlighting);
                _edge_highlighting = DrawLine(_graph.V[fss.I], _graph.V[fss.J], Brushes.Red);
                OutputCanvas.Children.Add(_edge_highlighting);
            }
            catch
            {
                // ignored
            }
        }

        private void freespacestrip_Click(object sender, RoutedEventArgs e)
        {
            //FreeSpaceStrip fss = ((FreeSpaceStrip)sender);

            //Graph graph = new Graph(2);
            //graph.V[0] = _graph.V[fss.I];
            //graph.V[1] = _graph.V[fss.J];
            //graph.E[0].Add(1);
            ////graph.E[1].Add(0);

            //_graph = graph;

            //Calculation();


            //ReDraw(true);
        }

        private void calculate_Click(object sender, RoutedEventArgs e)
        {
            if (!initialized)
                init();

            if (_path == null)
            {
                MessageBox.Show("Geen input indexPath.");
                return;
            }

            CalculateResult();

        }
        #endregion

        private CombinedGeometry geom = null;
        private System.Windows.Shapes.Path viewpath = null;
        private void Result_Click(object sender, RoutedEventArgs e)
        {


            if (_path == null || _path.Size <= 0)
            {
                MessageBox.Show("Press calculate first");
                return;
            }

            if (pg4 == null)
            {
                MessageBox.Show("Press symmetric button first");
                return;
            }

            // Check if indexPath exists otherwise create
            string path = String.Format("{0}/grid_{1}/{2}/pos{3:D2}", outputfolder, _graph.GridSize(), currentFile, _current_position);
            if (!Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            // Only input
            SaveAsImage(currentFile);

            //Calculation();

            /*
            canvas.Children.Clear();
            Draw(_path, Brushes.Black);
            DrawResult(_result);

            if (_result != null)
            {
                freeSpaceStack.Children.Clear();
                Interval result = _result;
                while (result != null && result.PathPointer != null)
                {
                    FreeSpaceStrip freeSpaceStrip = new FreeSpaceStrip(result.PathPointer.GraphIndex, result.GraphIndex, _size);
                    freeSpaceStrip.MouseEnter += freespacestrip_MouseEnter;
                    freeSpaceStrip.MouseLeftButtonDown += freespacestrip_Click;

                    freeSpaceStrip.wbmp = BitmapFactory.New(_size * _path.Size, _size);
                    freeSpaceStrip.wbmp.Clear(Colors.Gray);
                    freeSpaceStrip.imgControl.Source = freeSpaceStrip.wbmp;

                    DrawFreeSpaceDiagram(freeSpaceStrip);8
                    DrawIntervals(freeSpaceStrip);
                    DrawLeftPointers(freeSpaceStrip);
                    DrawRightPointers(freeSpaceStrip);
                    freeSpaceStack.Children.Add(freeSpaceStrip);

                    result = result.PathPointer;
                }
            }

            //if (_result != null)
            //{
            //    _graph = CreateResultGraph(_result);
            //}
            */




            //Console.WriteLine("Frechet distance: {0}", CalculateFrechetDistance());



        }

        private void SaveAsImage(string filename)
        {
            // Render output
            RenderTargetBitmap gridRTB = new RenderTargetBitmap((int)GridCanvas.RenderSize.Width, (int)GridCanvas.RenderSize.Height, 96d, 96d, PixelFormats.Pbgra32);
            RenderTargetBitmap inputRTB = new RenderTargetBitmap((int)InputCanvas.RenderSize.Width, (int)InputCanvas.RenderSize.Height, 96d, 96d, PixelFormats.Pbgra32);
            RenderTargetBitmap outputPolygonRTB = new RenderTargetBitmap((int)OutputPolygonCanvas.RenderSize.Width, (int)OutputPolygonCanvas.RenderSize.Height, 96d, 96d, PixelFormats.Pbgra32);
            RenderTargetBitmap outputRTB = new RenderTargetBitmap((int)OutputCanvas.RenderSize.Width, (int)OutputCanvas.RenderSize.Height, 96d, 96d, PixelFormats.Pbgra32);
            RenderTargetBitmap detectRTB = new RenderTargetBitmap((int)DetectCanvas.RenderSize.Width, (int)DetectCanvas.RenderSize.Height, 96d, 96d, PixelFormats.Pbgra32);

            gridRTB.Render(GridCanvas);
            inputRTB.Render(InputCanvas);
            outputPolygonRTB.Render(OutputPolygonCanvas);
            outputRTB.Render(OutputCanvas);
            detectRTB.Render(DetectCanvas);

            var gridCrop = new CroppedBitmap(gridRTB, new Int32Rect(0, 0, Convert.ToInt32(_graph.GridSize() * scale), Convert.ToInt32(_graph.GridSize() * scale)));
            var inputCrop = new CroppedBitmap(inputRTB, new Int32Rect(0, 0, Convert.ToInt32(_graph.GridSize() * scale), Convert.ToInt32(_graph.GridSize() * scale)));
            var outputPolygonCrop = new CroppedBitmap(outputPolygonRTB, new Int32Rect(0, 0, Convert.ToInt32(_graph.GridSize() * scale), Convert.ToInt32(_graph.GridSize() * scale)));
            var outputCrop = new CroppedBitmap(outputRTB, new Int32Rect(0, 0, Convert.ToInt32(_graph.GridSize() * scale), Convert.ToInt32(_graph.GridSize() * scale)));
            var detectCrop = new CroppedBitmap(detectRTB, new Int32Rect(0, 0, Convert.ToInt32(_graph.GridSize() * scale), Convert.ToInt32(_graph.GridSize() * scale)));

            //Combine the images here
            var id1 = new ImageDrawing(gridCrop, new Rect(0, 0, gridCrop.Width, gridCrop.Height));
            var id2 = new ImageDrawing(inputCrop, new Rect(0, 0, inputCrop.Width, inputCrop.Height));
            var id5 = new ImageDrawing(outputPolygonCrop, new Rect(0, 0, outputPolygonCrop.Width, outputPolygonCrop.Height));
            var id3 = new ImageDrawing(outputCrop, new Rect(0, 0, outputCrop.Width, outputCrop.Height));
            var id4 = new ImageDrawing(detectCrop, new Rect(0, 0, detectCrop.Width, detectCrop.Height));

            var dg = new DrawingGroup();
            dg.Children.Add(id1);
            dg.Children.Add(id2);
            dg.Children.Add(id5);
            dg.Children.Add(id3);
            dg.Children.Add(id4);

            var combinedImg = new RenderTargetBitmap((int)gridCrop.Width, (int)gridCrop.Height, 96, 96, PixelFormats.Pbgra32);
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawDrawing(dg);
            }
            combinedImg.Render(dv);

            BitmapEncoder pngEncoder;
            string path = String.Format("{0}/grid_{1}/{2}/pos{3:D2}", outputfolder, _graph.GridSize(), currentFile, _current_position);

            pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(inputCrop));
            using (var fs = System.IO.File.OpenWrite(string.Format("{0}/input.png", path)))
            {
                pngEncoder.Save(fs);
            }

            pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(outputPolygonCrop));
            using (var fs = System.IO.File.OpenWrite(string.Format("{0}/outputPolygon.png", path)))
            {
                pngEncoder.Save(fs);
            }

            pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(outputCrop));
            using (var fs = System.IO.File.OpenWrite(string.Format("{0}/output.png", path)))
            {
                pngEncoder.Save(fs);
            }

            pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(detectCrop));
            using (var fs = System.IO.File.OpenWrite(string.Format("{0}/detect.png", path)))
            {
                pngEncoder.Save(fs);
            }
            SaveOutput(string.Format("{0}/output.txt", path));

            double minimal = pg0.GetArea();
            minimal = Math.Min(minimal, pg1.GetArea());
            minimal = Math.Min(minimal, pg2.GetArea());
            minimal = Math.Min(minimal, pg3.GetArea());
            minimal = Math.Min(minimal, pg4.GetArea());
            minimal = Math.Min(minimal, pg5.GetArea());
            minimal = Math.Min(minimal, pg6.GetArea());
            minimal = Math.Min(minimal, pg7.GetArea());
            minimal = Math.Min(minimal, pg8.GetArea());

            // Store results to 
            string resultString = String.Format("{0},{1},{2},{3},{4},{5}", filename, epsilon.ToString(CultureInfo.CreateSpecificCulture("en-US")), minimal.ToString(CultureInfo.CreateSpecificCulture("en-US")), reusedVertices,
                reusedEdges, intersections);
            File.WriteAllText(string.Format("{0}/result.csv", path), resultString);

            pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(combinedImg));
            using (var fs = System.IO.File.OpenWrite(string.Format("{0}/full.png", path)))
            {
                pngEncoder.Save(fs);
            }

            dg = new DrawingGroup();
            dg.Children.Add(id1);
            dg.Children.Add(id2);
            dg.Children.Add(id3);

            combinedImg = new RenderTargetBitmap((int)gridCrop.Width, (int)gridCrop.Height, 96, 96, PixelFormats.Pbgra32);
            dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.DrawDrawing(dg);
            }
            combinedImg.Render(dv);

            pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(combinedImg));
            using (var fs = System.IO.File.OpenWrite(string.Format("{0}/result.png", path)))
            {
                pngEncoder.Save(fs);
            }


        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            init();

            Draw(GridCanvas, _graph, Brushes.LightGray);
            Draw(InputCanvas, _path, Brushes.Black);
        }

        private void MenuItemClear_Click(object sender, RoutedEventArgs e)
        {
            _path = new Path(0);

            _result = null;

            lblEpsilon.Content = '-';

            InputCanvas.Children.Clear();
            OutputCanvas.Children.Clear();
            DetectCanvas.Children.Clear();
        }

        private void DirectSameEdge_Unchecked(object sender, RoutedEventArgs e)
        {
            _allowSameEdge = false;

            OutputPolygonCanvas.Children.Clear();
            DetectCanvas.Children.Clear();
            OutputCanvas.Children.Clear();
            RenderCanvas.Children.Clear();
        }

        private void DirectSameEdge_Checked(object sender, RoutedEventArgs e)
        {
            _allowSameEdge = true;

            //ReDraw();
            OutputCanvas.Children.Clear();
            OutputPolygonCanvas.Children.Clear();
            DetectCanvas.Children.Clear();
            RenderCanvas.Children.Clear();
        }

        private void MenuItemSave_Click(object sender, RoutedEventArgs e)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < _path.V.Length; i++)
            {
                stringBuilder.AppendLine(String.Format("{0} {1}",
                    _path.V[i].X.ToString(CultureInfo.CreateSpecificCulture("en-US")),
                    _path.V[i].Y.ToString(CultureInfo.CreateSpecificCulture("en-US"))));
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt";
            if (saveFileDialog.ShowDialog() == true)
                File.WriteAllText(saveFileDialog.FileName, stringBuilder.ToString());
        }

        private void SaveResult(String filename)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (_resultGraph != null)
            {

            }
        }

        private void SaveOutput(String filename)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (_resultGraph != null)
            {
                for (int i = 0; i < _resultGraph.Size; i++)
                {
                    stringBuilder.AppendLine(String.Format("{0} {1}",
                        _resultGraph.V[i].X.ToString(CultureInfo.CreateSpecificCulture("en-US")),
                        _resultGraph.V[i].Y.ToString(CultureInfo.CreateSpecificCulture("en-US"))));
                }
                File.WriteAllText(filename, stringBuilder.ToString());
            }
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Text file (*.txt)|*.txt|Ipe file (*.ipe)|*.ipe";
            if (openFileDialog.ShowDialog() == true)
            {
                string extension = System.IO.Path.GetExtension(openFileDialog.FileName);

                IFileReader fileReader = null;
                if (extension == ".ipe")
                {
                    fileReader = new IPEReader();
                }
                else
                {
                    fileReader = new TXTReader();
                }

                _deltaX = 0;
                _deltaY = 0;
                _current_position = 0;
                lbl_curpos.Content = String.Format("current position: {0}", _current_position);


                _path = fileReader.ReadFile(openFileDialog.FileName);

                FileInfo file = new FileInfo(openFileDialog.FileName);
                currentFile = System.IO.Path.GetFileNameWithoutExtension(file.Name);

                InputCanvas.Children.Clear();
                OutputCanvas.Children.Clear();
                DetectCanvas.Children.Clear();
                OutputPolygonCanvas.Children.Clear();
                RenderCanvas.Children.Clear();
                DebugCanvas.Children.Clear();

                Normalize();
                Center();

                // Generate input polygon
                inputPolygon = new Polygon();
                foreach (Vertex v in _path.V)
                {
                    inputPolygon.Points.Add(new Point(v.X, v.Y));
                }

                Draw(InputCanvas, _path, Brushes.Black);


                //   Calculation();

                // ReDraw();
            }
        }

        private void Normalize()
        {
            if (_path == null || _path.Size <= 0)
                return;

            // Scale
            float xMin, xMax, yMin, yMax;
            xMin = xMax = _path.V.First().X;
            yMin = yMax = _path.V.First().Y;
            foreach (Vertex v in _path.V)
            {
                xMin = xMin < v.X ? xMin : v.X;
                xMax = xMax > v.X ? xMax : v.X;
                yMin = yMin < v.Y ? yMin : v.Y;
                yMax = yMax > v.Y ? yMax : v.Y;
            }

            // Scale factor
            float hScale = (_graph.GridSize() - 6) / (xMax - xMin);
            float vScale = (_graph.GridSize() - 6) / (yMax - yMin);

            float scale = hScale < vScale ? hScale : vScale;

            Console.WriteLine("scale: {0}", scale);

            // Scale
            foreach (Vertex v in _path.V)
            {
                v.X *= scale;
                v.Y *= scale;
            }

            Center();
        }

        private void Center()
        {
            if (_path == null)
                return;

            float xMin, xMax, yMin, yMax;
            xMin = xMax = _path.V.First().X;
            yMin = yMax = _path.V.First().Y;
            foreach (Vertex v in _path.V)
            {
                xMin = xMin < v.X ? xMin : v.X;
                xMax = xMax > v.X ? xMax : v.X;
                yMin = yMin < v.Y ? yMin : v.Y;
                yMax = yMax > v.Y ? yMax : v.Y;
            }

            // Center
            Vertex center = new Vertex((xMin + xMax) / 2, (yMin + yMax) / 2);
            foreach (Vertex v in _path.V)
            {
                v.X -= center.X - ((_graph.GridSize() - 6) / 2) - 3.0f;
                v.Y -= center.Y - ((_graph.GridSize() - 6) / 2) - 3.0f;
            }
        }

        private void MenuItemGrid_Click(object sender, RoutedEventArgs e)
        {
            GridBuilderWindow gridBuilderWindow = new GridBuilderWindow();
            if (gridBuilderWindow.ShowDialog() == true)
            {
                _graph = gridBuilderWindow.Grid;

                GridCanvas.Children.Clear();
                Draw(GridCanvas, _graph, Brushes.LightGray);

                Calculation(_graph, _path);

                ReDraw();
            }
        }

        private CombinedGeometry cg0, cg1, cg2, cg3, cg4, cg5, cg6, cg7, cg8;

        private void Clear()
        {
            _result = null;
            _path = null;
            indexPath = null;
            outputPolygon4 = null;
            pg4 = null;

            GridCanvas.Children.Clear();
            InputCanvas.Children.Clear();
            OutputCanvas.Children.Clear();
            DetectCanvas.Children.Clear();
            OutputPolygonCanvas.Children.Clear();
            RenderCanvas.Children.Clear();

            Draw(GridCanvas, _graph, Brushes.LightGray);
        }

        private void grid10_Click(object sender, RoutedEventArgs e)
        {
            size = 10;
            _graph = new GridGraph(10, 1f);
            scale = 500.0f / _graph.GridSize();
            scale *= 1.5f;
            Clear();
        }

        private void grid30_Click(object sender, RoutedEventArgs e)
        {
            size = 30;
            _graph = new GridGraph(30, 1f);
            scale = 500.0f / _graph.GridSize();
            scale *= 1.5f;
            Clear();
        }

        private void grid50_Click(object sender, RoutedEventArgs e)
        {
            size = 50;
            _graph = new GridGraph(50, 1f);
            scale = 500.0f / _graph.GridSize();
            scale *= 1.5f;
            Clear();
        }

        private void grid70_Click(object sender, RoutedEventArgs e)
        {
            size = 70;
            _graph = new GridGraph(70, 1f);
            scale = 500.0f / _graph.GridSize();
            scale *= 1.5f;
            Clear();
        }

        private void BtnFreespaceDiagram_Click(object sender, RoutedEventArgs e)
        {
            MapMatching mapMatching=new MapMatching();
            _freeSpaceStrips = mapMatching.GenerateFreeSpaceStrips(_resultGraph, _path, _size, epsilon);
            FreeSpaceDiagram freeSpaceDiagram = new FreeSpaceDiagram();
            for (int i = 0; i < _resultGraph.Size; i++)
            {
                for (int s = 0; s < _freeSpaceStrips[i].Count; s++)
                {
                    freeSpaceDiagram.freeSpaceStack.Children.Insert(0, _freeSpaceStrips[i][s]);
                }
            }
            freeSpaceDiagram.Show();

        }

        private void grid90_Click(object sender, RoutedEventArgs e)
        {
            size = 90;
            _graph = new GridGraph(90, 1f);
            scale = 500.0f / _graph.GridSize();
            scale *= 1.5f;
            Clear();
        }

        private PathGeometry pg0, pg1, pg2, pg3, pg4, pg5, pg6, pg7, pg8;
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (outputPolygon4 == null)
            {

                MessageBox.Show("Please calculate first");
                return;
            }

            cg0 = new CombinedGeometry(inputPolygon.RenderedGeometry, outputPolygon0.RenderedGeometry);
            cg0.GeometryCombineMode = GeometryCombineMode.Xor;
            pg0 = cg0.GetFlattenedPathGeometry();

            cg1 = new CombinedGeometry(inputPolygon.RenderedGeometry, outputPolygon1.RenderedGeometry);
            cg1.GeometryCombineMode = GeometryCombineMode.Xor;
            pg1 = cg1.GetFlattenedPathGeometry();

            cg2 = new CombinedGeometry(inputPolygon.RenderedGeometry, outputPolygon2.RenderedGeometry);
            cg2.GeometryCombineMode = GeometryCombineMode.Xor;
            pg2 = cg2.GetFlattenedPathGeometry();

            cg3 = new CombinedGeometry(inputPolygon.RenderedGeometry, outputPolygon3.RenderedGeometry);
            cg3.GeometryCombineMode = GeometryCombineMode.Xor;
            pg3 = cg3.GetFlattenedPathGeometry();

            cg4 = new CombinedGeometry(inputPolygon.RenderedGeometry, outputPolygon4.RenderedGeometry);
            cg4.GeometryCombineMode = GeometryCombineMode.Xor;
            pg4 = cg4.GetFlattenedPathGeometry();

            cg5 = new CombinedGeometry(inputPolygon.RenderedGeometry, outputPolygon5.RenderedGeometry);
            cg5.GeometryCombineMode = GeometryCombineMode.Xor;
            pg5 = cg5.GetFlattenedPathGeometry();

            cg6 = new CombinedGeometry(inputPolygon.RenderedGeometry, outputPolygon6.RenderedGeometry);
            cg6.GeometryCombineMode = GeometryCombineMode.Xor;
            pg6 = cg6.GetFlattenedPathGeometry();

            cg7 = new CombinedGeometry(inputPolygon.RenderedGeometry, outputPolygon7.RenderedGeometry);
            cg7.GeometryCombineMode = GeometryCombineMode.Xor;
            pg7 = cg7.GetFlattenedPathGeometry();

            cg8 = new CombinedGeometry(inputPolygon.RenderedGeometry, outputPolygon8.RenderedGeometry);
            cg8.GeometryCombineMode = GeometryCombineMode.Xor;
            pg8 = cg8.GetFlattenedPathGeometry();

            System.Windows.Shapes.Path combinedPath = new System.Windows.Shapes.Path();
            combinedPath.Data = cg4;
            combinedPath.Fill = Brushes.Red;
            Console.Out.WriteLine("Output area: {0}", inputPolygon.RenderedGeometry.GetArea());
            Console.Out.WriteLine("Symmetric difference: {0}", cg4.GetArea());
            Console.Out.WriteLine("Symmetric difference: {0}", pg4.GetArea());
            combinedPath.LayoutTransform = new ScaleTransform(scale, scale, 0, 0);
            RenderCanvas.Children.Add(combinedPath);
        }

        private int _current_position = 0;
        private float _deltaX = 0f;
        private float _deltaY = 0f;
        private void nextPosition_click(object sender, RoutedEventArgs e)
        {
            if (_path == null)
                return;

            _current_position++;

            if (_current_position >= 25)
                _current_position = 0;

            lbl_curpos.Content = String.Format("current position: {0}", _current_position);

            float deltaX = (0.25f) * (_current_position % 5);
            float deltaY = (0.25f) * (float)(Math.Floor(_current_position / 5.0));
            foreach (Vertex v in _path.V)
            {
                v.X += deltaX - _deltaX;
                v.Y += deltaY - _deltaY;
            }

            _deltaX = deltaX;
            _deltaY = deltaY;

            // Generate input polygon
            inputPolygon = new Polygon();
            foreach (Vertex v in _path.V)
            {
                inputPolygon.Points.Add(new Point(v.X, v.Y));
            }

            // Reset output etc
            _result = null;
            indexPath = null;
            outputPolygon4 = null;
            pg4 = null;

            InputCanvas.Children.Clear();
            OutputCanvas.Children.Clear();
            DetectCanvas.Children.Clear();
            OutputPolygonCanvas.Children.Clear();
            RenderCanvas.Children.Clear();

            Draw(InputCanvas, _path, Brushes.Black);

        }
    }
}