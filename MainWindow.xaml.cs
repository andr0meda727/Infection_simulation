using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32; // To jest kluczowe dla SaveButton/LoadButton

namespace InfectionSimulation
{
    public partial class MainWindow : Window
    {
        // Zamiast Timera używamy CancellationToken do zatrzymywania wątku
        private CancellationTokenSource _cts;
        private List<Individual> _individuals;
        private double _simulationTime;
        private int _nextId;
        private bool _hasImmunity;
        private Random _random;

        private const int WIDTH = 800; // pixele (na metry 800 / 10), bo 10px = 1m u mnie
        private const int HEIGHT = 600;
        private const double PIXELS_PER_METER = 10.0; // 1 metr = 10 pixels
        private const double FIXED_DT = 1.0 / 25.0;   // 25 krokow na sekundę (0.04s)

        // Obiekt do synchronizacji listy osobników między UI a wątkiem logicznym
        private readonly object _simulationLock = new object();

        public MainWindow()
        {
            InitializeComponent();
            _random = new Random();
            _individuals = new List<Individual>();
            _simulationTime = 0;
            _nextId = 0;
            _hasImmunity = false;

            InitializeSimulation(false);
        }

        private void StartSimulationLoop()
        {
            _cts?.Cancel(); // Anuluj poprzednią pętlę, jeśli istnieje
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    // 1) Logika - zawsze z FIXED_DT
                    UpdateLogic(FIXED_DT);

                    // 2) UI - odświeżamy na wątku UI
                    if (!token.IsCancellationRequested)
                    {
                        try
                        {
                            await this.Dispatcher.InvokeAsync(() => UpdateVisuals());
                        }
                        catch (TaskCanceledException) { }
                    }

                    // 3) Czekamy do następnego kroku (dążymy do stałego kroku 25/s)
                    // Task.Delay może być minimalnie niedokładne, ale jest wystarczające do 25 Hz.
                    await Task.Delay(TimeSpan.FromSeconds(FIXED_DT), token);
                }
            }, token);

        }


        private void UpdateLogic(double deltaTime)
        {
            List<Individual> snapshot;

            // Robimy szybką kopię referencji, żeby operować na liście bezpiecznie
            // (w tym czasie UI nie może dodawać/usuwać obiektów)
            lock (_simulationLock)
            {
                snapshot = new List<Individual>(_individuals);
            }

            // PARALLEL FOREACH - To jest klucz do wydajności obliczeń
            // Przetwarzamy każdego osobnika na osobnych wątkach procesora
            Parallel.ForEach(snapshot, individual =>
            {
                // Przekazujemy snapshot jako "allIndividuals", żeby unikać błędów modyfikacji kolekcji
                individual.Update(deltaTime, WIDTH, HEIGHT, snapshot);
            });

            // Usuwanie i spawnowanie musi być synchroniczne (na jednym wątku),
            // bo modyfikuje listę. Robimy to po zakończeniu Parallel.ForEach
            lock (_simulationLock)
            {
                // Usuń oznaczonych
                _individuals.RemoveAll(x => x.IsDeadOrLeft);

                // Spawnowanie nowych
                if (_random.NextDouble() < 0.05 && _individuals.Count < 100) // Zwiększyłem limit
                {
                    SpawnIndividual(); // SpawnIndividual musi być bezpieczne (nie rysuje od razu!)
                }

                _simulationTime += deltaTime;
            }
        }

        private void UpdateVisuals()
        {
            // Ta metoda działa na wątku UI, więc może bezpiecznie dotykać Canvasa

            // 1. Synchronizacja listy obiektów z Canvasem
            // Najprostsza metoda (można zoptymalizować Dictionary, ale przy <500 obiektach wystarczy)

            // Usuwanie wizualizacji dla nieistniejących obiektów
            var ellipsesToRemove = new List<UIElement>();

            // Tworzymy HashSet IDków dla szybkiego sprawdzania
            HashSet<int> activeIds;
            lock (_simulationLock)
            {
                activeIds = _individuals.Select(i => i.Id).ToHashSet();
            }

            foreach (Ellipse child in SimulationCanvas.Children.OfType<Ellipse>())
            {
                if (child.Tag is int id && !activeIds.Contains(id))
                {
                    ellipsesToRemove.Add(child);
                }
            }
            foreach (var el in ellipsesToRemove) SimulationCanvas.Children.Remove(el);

            // Aktualizacja i dodawanie
            lock (_simulationLock)
            {
                foreach (var individual in _individuals)
                {
                    // Znajdź istniejącą kulkę
                    var ellipse = SimulationCanvas.Children
                        .OfType<Ellipse>()
                        .FirstOrDefault(e => (int)e.Tag == individual.Id);

                    // Jeśli nie ma - stwórz
                    if (ellipse == null)
                    {
                        ellipse = new Ellipse
                        {
                            Width = 12,
                            Height = 12,
                            Tag = individual.Id
                        };
                        SimulationCanvas.Children.Add(ellipse);
                    }

                    // Aktualizuj pozycję i kolor
                    // Pobieramy kolor ze State (upewnij się że State.GetColor jest szybkie)
                    ellipse.Fill = new SolidColorBrush(individual.State.GetColor());
                    Canvas.SetLeft(ellipse, individual.Position.X - 6);
                    Canvas.SetTop(ellipse, individual.Position.Y - 6);
                }

                UpdateStats();
            }
        }

        // Zmodyfikowana inicjalizacja
        private void InitializeSimulation(bool withImmunity)
        {
            lock (_simulationLock)
            {
                _individuals.Clear();
            }
            SimulationCanvas.Children.Clear();
            _simulationTime = 0;
            _nextId = 0;
            _hasImmunity = withImmunity;

            int initialCount = 50; // Możemy dać więcej na start
            for (int i = 0; i < initialCount; i++)
            {
                // ... (logika spawnowania identyczna jak w SpawnIndividual)
                SpawnIndividualInternal(true); // Helper method
            }

            UpdateStats();
        }

        // Helper do spawnowania bez rysowania (rysowanie robi UpdateVisuals)
        private void SpawnIndividual()
        {
            SpawnIndividualInternal(false);
        }

        private void SpawnIndividualInternal(bool initialParams = false)
        {
            // Logika wyboru pozycji (skopiowana z Twojego kodu)
            double x = 0, y = 0;

            if (initialParams) {
                x = _random.NextDouble() * WIDTH;
                y = _random.NextDouble() * HEIGHT;
            } else {
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
            var individual = new Individual(_nextId++, x, y, isImmune);

            if (!isImmune && _random.NextDouble() < 0.1)
            {
                individual.State = _random.NextDouble() < 0.5
                    ? (IIndividualState)new InfectedAsymptomaticState()
                    : new InfectedSymptomaticState();
            }

            // Dodajemy tylko do listy logicznej!
            // Canvas zaktualizuje się sam w UpdateVisuals
            _individuals.Add(individual);
        }


        // Button Handlers

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
        
        private void UpdateStats()
        {
            // Ta metoda jest wywoływana wewnątrz UpdateVisuals, która posiada już locka,
            // więc tutaj lock nie jest konieczny, ale dostęp do _individuals jest bezpieczny.
            int healthy = 0, infected = 0, immune = 0;

            foreach (var individual in _individuals)
            {
                if (individual.State is HealthyState) healthy++;
                else if (individual.State is InfectedAsymptomaticState ||
                         individual.State is InfectedSymptomaticState) infected++;
                else if (individual.State is ImmuneState) immune++;
            }

            TimeText.Text = $"Czas: {_simulationTime:F1}s";
            PopulationText.Text = $"Populacja: {_individuals.Count}";
            HealthyText.Text = $"Zdrowi: {healthy}";
            InfectedText.Text = $"Zarażeni: {infected}";
            ImmuneText.Text = $"Odporni: {immune}";
        }

        private void ImmunityCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Zatrzymujemy pętlę przy zmianie ustawień
            _cts?.Cancel();
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
            InitializeSimulation(ImmunityCheckBox.IsChecked ?? false);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Zatrzymujemy symulację przed zapisem, żeby stan był spójny
            _cts?.Cancel();
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;

            SimulationMemento memento;

            // Używamy locka, aby bezpiecznie odczytać listę
            lock (_simulationLock)
            {
                memento = new SimulationMemento
                {
                    Individuals = _individuals.Select(i => i.SaveState()).ToList(),
                    SimulationTime = _simulationTime,
                    HasImmunity = _hasImmunity,
                    NextId = _nextId
                };
            }

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
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Zatrzymujemy obecną pętlę
                    _cts?.Cancel();

                    var json = File.ReadAllText(dialog.FileName);
                    var memento = JsonSerializer.Deserialize<SimulationMemento>(json);

                    // Sekcja krytyczna - modyfikujemy listę i stan
                    lock (_simulationLock)
                    {
                        _individuals.Clear();
                        // SimulationCanvas.Children.Clear(); // To zrobi UpdateVisuals automatycznie

                        foreach (var indMemento in memento.Individuals)
                        {
                            var individual = Individual.RestoreState(indMemento, _random);
                            _individuals.Add(individual);
                        }

                        _simulationTime = memento.SimulationTime;
                        _hasImmunity = memento.HasImmunity;
                        _nextId = memento.NextId;
                    }

                    ImmunityCheckBox.IsChecked = _hasImmunity;

                    // Ręczne odświeżenie widoku po załadowaniu
                    UpdateVisuals();

                    StartButton.IsEnabled = true;
                    PauseButton.IsEnabled = false;

                    MessageBox.Show("Stan wczytany!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd wczytywania: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}