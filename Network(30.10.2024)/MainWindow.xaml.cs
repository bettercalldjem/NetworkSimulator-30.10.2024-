using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;

namespace PacketNetworkSimulation
{
    public partial class MainWindow : Window
    {
        private List<NetworkNode> nodes;
        private int packetId = 0;
        private Random rand = new Random();
        private CancellationTokenSource cancellationTokenSource;
        private NetworkNode selectedNode; // Определение переменной для выбранного узла
        private Point offset;

        public MainWindow()
        {
            InitializeComponent();
            InitializeNetwork();
            DrawNetwork();
        }

        private void InitializeNetwork()
        {
            nodes = new List<NetworkNode>();
        }

        private void DrawNetwork()
        {
            NetworkCanvas.Children.Clear();
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var ellipse = new Ellipse
                {
                    Width = 40,
                    Height = 40,
                    Fill = Brushes.Blue,
                    Name = "Node_" + i,
                    Tag = node
                };
                ellipse.MouseDown += Node_MouseDown;
                Canvas.SetLeft(ellipse, node.Position.X);
                Canvas.SetTop(ellipse, node.Position.Y);
                NetworkCanvas.Children.Add(ellipse);

                var textBlock = new TextBlock
                {
                    Text = node.IP,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(textBlock, node.Position.X + 10);
                Canvas.SetTop(textBlock, node.Position.Y + 45);
                NetworkCanvas.Children.Add(textBlock);
            }
        }

        private void Node_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                selectedNode = (sender as Ellipse).Tag as NetworkNode;
                offset = e.GetPosition(NetworkCanvas);
                offset.X -= Canvas.GetLeft(sender as Ellipse);
                offset.Y -= Canvas.GetTop(sender as Ellipse);
                MouseMove += Node_MouseMove;
                MouseUp += Node_MouseUp;
            }
        }

        private void Node_MouseMove(object sender, MouseEventArgs e)
        {
            if (selectedNode != null)
            {
                var position = e.GetPosition(NetworkCanvas);
                Canvas.SetLeft((UIElement)sender, position.X - offset.X);
                Canvas.SetTop((UIElement)sender, position.Y - offset.Y);
            }
        }

        private void Node_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (selectedNode != null)
            {
                selectedNode.Position = new Point(Canvas.GetLeft((UIElement)sender), Canvas.GetTop((UIElement)sender));
                selectedNode = null;
                MouseMove -= Node_MouseMove;
                MouseUp -= Node_MouseUp;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                selectedNode = null; // Убедитесь, что никакой узел не выбран
            }
        }

        private void StartSimulation_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;

            Task.Run(() =>
            {
                while (packetId < 100 && !token.IsCancellationRequested)
                {
                    foreach (var node in nodes)
                    {
                        if (!node.IsOverloaded)
                        {
                            var priority = rand.Next(1, 4); // 1 - критический, 2 - обычный, 3 - низкий
                            var packet = new NetworkPacket(packetId++, rand.Next(1, 10), "Text", (PriorityLevel)priority, "192.168.1.1", "192.168.1.2");
                            node.AddPacket(packet);
                        }
                    }
                    UpdateNetworkVisualization();
                    Thread.Sleep(500);

                    // Проверка на перегрузку всех узлов
                    if (nodes.All(n => n.IsOverloaded))
                    {
                        Dispatcher.Invoke(() =>
                            MessageBox.Show("Все узлы перегружены!"));
                        break;
                    }
                }

                foreach (var node in nodes)
                {
                    node.StartProcessing(UpdateNetworkVisualization);
                }
            }, token);
        }

        private void StopSimulation_Click(object sender, RoutedEventArgs e)
        {
            cancellationTokenSource?.Cancel();
            foreach (var node in nodes)
            {
                node.StopProcessing();
            }
            StatusText.Text = "Симуляция остановлена.";
        }

        private void UpdateNetworkVisualization()
        {
            Dispatcher.Invoke(() =>
            {
                StatusText.Text = $"Отправлено пакетов: {packetId}";

                foreach (var node in nodes)
                {
                    var nodeEllipse = NetworkCanvas.Children
                        .OfType<Ellipse>()
                        .FirstOrDefault(e => e.Name == "Node_" + nodes.IndexOf(node));

                    if (nodeEllipse != null)
                    {
                        nodeEllipse.Fill = node.IsOverloaded ? Brushes.Red : Brushes.Blue;
                    }
                }
            });
        }

        private void AddNode_Click(object sender, RoutedEventArgs e)
        {
            var ipAddress = "192.168.1." + (nodes.Count + 1);
            var newNode = new NetworkNode(ipAddress, rand.Next(5, 20))
            {
                Position = new Point(50 + nodes.Count * 150, 100) // Начальная позиция узла
            };
            nodes.Add(newNode);
            DrawNetwork();
        }

        private void RemoveNode_Click(object sender, RoutedEventArgs e)
        {
            if (nodes.Count > 0)
            {
                nodes.RemoveAt(nodes.Count - 1);
                DrawNetwork();
            }
        }

        private void AddConnection_Click(object sender, RoutedEventArgs e)
        {
            if (nodes.Count < 2) return;

            // Пример подключения первых двух узлов
            nodes[0].AddConnection(nodes[1]);
            MessageBox.Show($"Соединение добавлено между {nodes[0].IP} и {nodes[1].IP}");
        }

        private void RemoveConnection_Click(object sender, RoutedEventArgs e)
        {
            if (nodes.Count < 2) return;

            // Пример удаления соединения между первыми двумя узлами
            nodes[0].RemoveConnection(nodes[1]);
            MessageBox.Show($"Соединение удалено между {nodes[0].IP} и {nodes[1].IP}");
        }
    }

    public class NetworkPacket
    {
        public int Id { get; set; }
        public int Size { get; set; }
        public string DataType { get; set; }
        public PriorityLevel Priority { get; set; }
        public string SourceIP { get; set; }
        public string DestinationIP { get; set; }

        public NetworkPacket(int id, int size, string dataType, PriorityLevel priority, string sourceIP, string destinationIP)
        {
            Id = id;
            Size = size;
            DataType = dataType;
            Priority = priority;
            SourceIP = sourceIP;
            DestinationIP = destinationIP;
        }
    }

    public enum PriorityLevel
    {
        Critical,
        Normal,
        Low
    }

    public class NetworkNode
    {
        public string IP { get; set; }
        public ConcurrentQueue<NetworkPacket> PacketBuffer { get; set; }
        public List<NetworkNode> Connections { get; set; }
        public int Bandwidth { get; set; }
        public bool IsOverloaded { get; private set; }
        public Point Position { get; set; }

        private bool isProcessing;

        public NetworkNode(string ip, int bandwidth)
        {
            IP = ip;
            PacketBuffer = new ConcurrentQueue<NetworkPacket>();
            Connections = new List<NetworkNode>();
            Bandwidth = bandwidth;
            IsOverloaded = false;
            Position = new Point(0, 0);
        }

        public void AddConnection(NetworkNode node)
        {
            if (!Connections.Contains(node))
            {
                Connections.Add(node);
            }
        }

        public void RemoveConnection(NetworkNode node)
        {
            Connections.Remove(node);
        }

        public void AddPacket(NetworkPacket packet)
        {
            if (PacketBuffer.Count < Bandwidth)
            {
                PacketBuffer.Enqueue(packet);
            }
            else
            {
                IsOverloaded = true;
                GoToRest(); // "Выключаем" узел
                MessageBox.Show($"Узел {IP} перегружен и отключен!");
            }
        }

        public void GoToRest()
        {
            IsOverloaded = true;
            StopProcessing();
        }

        public void StartProcessing(Action updateNetworkVisualization)
        {
            isProcessing = true;
            Task.Run(() =>
            {
                while (isProcessing)
                {
                    if (PacketBuffer.TryDequeue(out NetworkPacket packet))
                    {
                        Thread.Sleep(1000); // Имитация обработки пакета
                    }
                    updateNetworkVisualization();
                }
            });
        }

        public void StopProcessing()
        {
            isProcessing = false;
        }
    }
}
