using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Infection_Simulation.Simulation;
using Microsoft.Win32;

namespace InfectionSimulation
{
    public partial class MainWindow : Window
    {
        private CancellationTokenSource? _cts;
        private readonly SimulationEngine _engine;
        private const double FIXED_DT = 1.0 / 25.0;

        public MainWindow()
        {
            InitializeComponent();
            _engine = new SimulationEngine();
            _engine.Initialize(false);
            UpdateVisuals(); // Pierwszy render
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
                    _engine.Update(FIXED_DT);
                    await Dispatcher.InvokeAsync(UpdateVisuals);
                    await Task.Delay(TimeSpan.FromSeconds(FIXED_DT), token);
                }
            }, token);
        }

        private void StopSimulation()
        {
            _cts?.Cancel();
            StartButton.IsEnabled = true;
            PauseButton.IsEnabled = false;
        }

        private void UpdateVisuals()
        {
            // 1. Usuń nieistniejące kulki
            var activeIds = _engine.Individuals.Select(i => i.Id).ToHashSet();
            var toRemove = SimulationCanvas.Children.OfType<Ellipse>()
                .Where(e => !activeIds.Contains((int)e.Tag))
                .ToList();

            foreach (var el in toRemove)
                SimulationCanvas.Children.Remove(el);

            // 2. Aktualizuj lub dodaj kulki
            foreach (var ind in _engine.Individuals)
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

        private void UpdateStats()
        {
            var stats = _engine.GetStats();

            TimeText.Text = $"Czas: {stats.Time:F1}s";
            PopulationText.Text = $"Populacja: {stats.Population}";
            HealthyText.Text = $"Zdrowi: {stats.Healthy}";
            InfectedText.Text = $"Zarażeni: {stats.Infected}";
            ImmuneText.Text = $"Odporni: {stats.Immune}";
        }

        // Button handlers

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartSimulationLoop();
            StartButton.IsEnabled = false;
            PauseButton.IsEnabled = true;
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            StopSimulation();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            StopSimulation();
            _engine.Initialize(ImmunityCheckBox.IsChecked ?? false);
            SimulationCanvas.Children.Clear();
            UpdateVisuals();
        }

        private void ImmunityCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            StopSimulation();
            _engine.Initialize(ImmunityCheckBox.IsChecked ?? false);
            SimulationCanvas.Children.Clear();
            UpdateVisuals();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            StopSimulation();

            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                FileName = SimulationSerializer.GenerateFileName()
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var memento = _engine.CreateMemento(); // memento
                    SimulationSerializer.Save(memento, dialog.FileName); // caretaker
                    MessageBox.Show("Stan zapisany!", "Sukces",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd zapisu: {ex.Message}", "Błąd",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                    StopSimulation();

                    var memento = SimulationSerializer.Load(dialog.FileName); // caretaker
                    _engine.RestoreMemento(memento); // memento

                    ImmunityCheckBox.IsChecked = memento.HasImmunity;
                    SimulationCanvas.Children.Clear();
                    UpdateVisuals();

                    MessageBox.Show("Stan wczytany!", "Sukces",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd wczytywania: {ex.Message}", "Błąd",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}