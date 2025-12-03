using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace InfectionSimulation
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource? _cts;
        private List<Individual> _individuals;
        private double _simulationTime;
        private int _nextId;
        private bool _hasImmunity;
        private readonly Random _random;

        // Rozmiary okna w pixelach
        // Przeliczając na metry w klasie Individual (10 pixeli = 1 metr), czyli plansza 80m x 60m
        private const int WIDTH = 800; 
        private const int HEIGHT = 600;
        private const double FIXED_DT = 1.0 / 25.0; // 25 kroków na sek

        public MainWindow()
        {
            InitializeComponent();
            _random = new Random();
            _individuals = new List<Individual>();
            InitializeSimulation(false);
        }

        private void StartSimulationLoop()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    UpdateLogic(FIXED_DT);

                    await Dispatcher.InvokeAsync(UpdateVisuals);
                    await Task.Delay(TimeSpan.FromSeconds(FIXED_DT), token);
                }
            }, token);
        }

        private void UpdateLogic(double deltaTime)
        {
            // Parallel update - bezpieczne bo każdy osobnik modyfikuje tylko siebie
            Parallel.ForEach(_individuals, ind =>
                ind.Update(deltaTime, WIDTH, HEIGHT, _individuals, _random));

            // Usuń osobników, którzy wyszli
            _individuals.RemoveAll(i => i.IsDeadOrLeft);

            // Dodaj nowych (5% szans co krok, max 100 osobników)
            if (_random.NextDouble() < 0.05 && _individuals.Count < 100)
            {
                SpawnIndividual();
            }

            _simulationTime += deltaTime;
        }

        private void UpdateVisuals()
        {
            // Usuń kulki dla nieistniejących osobników
            var activeIds = _individuals.Select(i => i.Id).ToHashSet();
            var toRemove = SimulationCanvas.Children.OfType<Ellipse>()
                .Where(e => !activeIds.Contains((int)e.Tag))
                .ToList();

            foreach (var el in toRemove)
                SimulationCanvas.Children.Remove(el);

            // Aktualizuj lub dodaj kulki
            foreach (var ind in _individuals)
            {
                var ellipse = SimulationCanvas.Children.OfType<Ellipse>()
                    .FirstOrDefault(e => (int)e.Tag == ind.Id);

                if (ellipse == null)
                {
                    ellipse = new Ellipse
                    {
                        Width = 12,
                        Height = 12,
                        Tag = ind.Id
                    };
                    SimulationCanvas.Children.Add(ellipse);
                }

                ellipse.Fill = new SolidColorBrush(ind.State.GetColor());
                Canvas.SetLeft(ellipse, ind.Position.X - 6);
                Canvas.SetTop(ellipse, ind.Position.Y - 6);
            }

            UpdateStats();
        }

        private void InitializeSimulation(bool withImmunity)
        {
            _individuals.Clear();
            SimulationCanvas.Children.Clear();
            _simulationTime = 0;
            _nextId = 0;
            _hasImmunity = withImmunity;

            for (int i = 0; i < 100; i++)
                SpawnIndividual(true);

            UpdateStats();
        }

        private void SpawnIndividual(bool isInitial = false)
        {
            double x = 0, y = 0;

            if (isInitial)
            {
                x = _random.NextDouble() * WIDTH;
                y = _random.NextDouble() * HEIGHT;
            }
            else
            {
                int side = _random.Next(4);
                switch (side)
                {
                    case 0: x = 0; y = _random.NextDouble() * HEIGHT; break;
                    case 1: x = WIDTH; y = _random.NextDouble() * HEIGHT; break;
                    case 2: x = _random.NextDouble() * WIDTH; y = 0; break;
                    case 3: x = _random.NextDouble() * WIDTH; y = HEIGHT; break;
                }
            }

            bool isImmune = _hasImmunity && _random.NextDouble() < 0.3;
            var individual = new Individual(_nextId++, x, y, isImmune, _random);

            if (!isImmune && _random.NextDouble() < 0.1)
            {
                individual.State = _random.NextDouble() < 0.5
                    ? new InfectedAsymptomaticState()
                    : new InfectedSymptomaticState();
            }

            _individuals.Add(individual);
        }

        private void UpdateStats()
        {
            int healthy = 0, infected = 0, immune = 0;

            foreach (var ind in _individuals)
            {
                if (ind.State is HealthyState) healthy++;
                else if (ind.State is InfectedAsymptomaticState or InfectedSymptomaticState) infected++;
                else if (ind.State is ImmuneState) immune++;
            }

            TimeText.Text = $"Czas: {_simulationTime:F1}s";
            PopulationText.Text = $"Populacja: {_individuals.Count}";
            HealthyText.Text = $"Zdrowi: {healthy}";
            InfectedText.Text = $"Zarażeni: {infected}";
            ImmuneText.Text = $"Odporni: {immune}";
        }

        // Buttons handlers

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartSimulationLoop();
            StartButton.IsEnabled = false;
            PauseButton.IsEnabled = true;
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            InitializeSimulation(ImmunityCheckBox.IsChecked ?? false);
        }

        private void ImmunityCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            InitializeSimulation(ImmunityCheckBox.IsChecked ?? false);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _cts?.Cancel();
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;

            var memento = new SimulationMemento
            {
                Individuals = _individuals.Select(i => i.SaveState()).ToList(),
                SimulationTime = _simulationTime,
                HasImmunity = _hasImmunity,
                NextId = _nextId
            };

            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = $"simulation_{DateTime.Now:yyyyMMdd_HHmmss}.json"
            };

            if (dialog.ShowDialog() == true)
            {
                var json = JsonSerializer.Serialize(memento, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dialog.FileName, json);
                MessageBox.Show("Stan zapisany!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "JSON files (*.json)|*.json" };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _cts?.Cancel();

                    var json = File.ReadAllText(dialog.FileName);
                    var memento = JsonSerializer.Deserialize<SimulationMemento>(json);

                    _individuals.Clear();
                    foreach (var m in memento.Individuals)
                        _individuals.Add(Individual.RestoreState(m, _random));

                    _simulationTime = memento.SimulationTime;
                    _hasImmunity = memento.HasImmunity;
                    _nextId = memento.NextId;

                    ImmunityCheckBox.IsChecked = _hasImmunity;
                    UpdateVisuals();

                    StartButton.IsEnabled = true;
                    PauseButton.IsEnabled = false;

                    MessageBox.Show("Stan wczytany!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}