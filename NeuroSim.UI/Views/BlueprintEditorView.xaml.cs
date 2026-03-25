// AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/NeuroSim.UI/Views/BlueprintEditorView.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NeuroSim.UI.Models.Blueprint;
using NeuroSim.UI.ViewModels;

namespace NeuroSim.UI.Views;

public partial class BlueprintEditorView : UserControl
{
    private Point _dragStart;
    private BlueprintNode? _draggingNode;
    private BlueprintNode? _potentialDragNode;
    private BlueprintNode? _connectionSource;
    private PortDefinition? _connectionSourcePort;
    private FrameworkElement? _connectionSourcePortElement;

    public BlueprintEditorView()
    {
        InitializeComponent();
    }

    private BlueprintViewModel? Vm => DataContext as BlueprintViewModel;

    private void BlockPalette_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not BlockDefinition def || Vm is null)
            return;
        DragDrop.DoDragDrop(fe, def, DragDropEffects.Copy);
    }

    private void Canvas_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(BlockDefinition)) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Canvas_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(BlockDefinition)) is not BlockDefinition def || Vm is null)
            return;

        var scroll = (ScrollViewer)sender;
        var canvas = (Canvas)scroll.Content;
        var pos = e.GetPosition(canvas);
        Vm.AddNodeFromPalette(def, pos);
        e.Handled = true;
    }

    private void BlockNode_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not BlueprintNode node || Vm is null)
            return;
        _potentialDragNode = node;
        _dragStart = e.GetPosition(null);
        e.Handled = true;
    }

    private void BlockNode_MouseMove(object sender, MouseEventArgs e)
    {
        if (_potentialDragNode is not null && e.LeftButton == MouseButtonState.Pressed)
        {
            var current = e.GetPosition(null);
            if (Math.Abs(current.X - _dragStart.X) + Math.Abs(current.Y - _dragStart.Y) > 5)
            {
                _draggingNode = _potentialDragNode;
                _potentialDragNode = null;
                (sender as FrameworkElement)?.CaptureMouse();
            }
        }
        if (_draggingNode is not null && e.LeftButton == MouseButtonState.Pressed)
        {
            var current = e.GetPosition(null);
            var delta = new Point(current.X - _dragStart.X, current.Y - _dragStart.Y);
            _draggingNode.X += delta.X;
            _draggingNode.Y += delta.Y;
            _dragStart = current;
        }
    }

    private void BlockNode_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_potentialDragNode is not null && Vm is not null)
        {
            Vm.SelectedNode = _potentialDragNode;
            _potentialDragNode = null;
        }
        if (_draggingNode is not null)
        {
            (sender as FrameworkElement)?.ReleaseMouseCapture();
            _draggingNode = null;
        }
    }

    private void OutputPort_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        var port = FindPortAndNode(fe, out var node);
        if (port is null || node is null || port.IsInput) return; // start from output only
        _connectionSource = node;
        _connectionSourcePort = port;
        _connectionSourcePortElement = fe;
        fe.CaptureMouse();
        e.Handled = true;
    }

    private void OutputPort_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_connectionSource is null || _connectionSourcePort is null) return;
        _connectionSourcePortElement?.ReleaseMouseCapture();
        _connectionSourcePortElement = null;

        var target = FindPortAndNodeUnderMouse(out var targetNode);
        if (target is not null && targetNode is not null && target.IsInput && targetNode != _connectionSource && Vm is not null)
            Vm.AddConnection(_connectionSource, _connectionSourcePort.Id, targetNode, target.Id);

        _connectionSource = null;
        _connectionSourcePort = null;
        e.Handled = true;
    }

    private void InputPort_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Input ports are targets; we don't start connections from them in this flow
        // (we could support bidirectional; for now output->input only)
    }

    private void InputPort_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // When releasing on an input port, we're completing a connection from an output
        if (_connectionSource is null || _connectionSourcePort is null) return;
        _connectionSourcePortElement?.ReleaseMouseCapture();
        _connectionSourcePortElement = null;

        var target = FindPortAndNode(sender as FrameworkElement, out var targetNode);
        if (target is not null && targetNode is not null && target.IsInput && targetNode != _connectionSource && Vm is not null)
            Vm.AddConnection(_connectionSource, _connectionSourcePort.Id, targetNode, target.Id);

        _connectionSource = null;
        _connectionSourcePort = null;
        e.Handled = true;
    }

    private static PortDefinition? FindPortAndNode(FrameworkElement? fe, out BlueprintNode? node)
    {
        node = null;
        if (fe is null) return null;
        var port = fe.DataContext as PortDefinition;
        var current = VisualTreeHelper.GetParent(fe) as FrameworkElement;
        while (current is not null)
        {
            if (current.DataContext is BlueprintNode n)
            {
                node = n;
                return port;
            }
            current = VisualTreeHelper.GetParent(current) as FrameworkElement;
        }
        return port;
    }

    private PortDefinition? FindPortAndNodeUnderMouse(out BlueprintNode? node)
    {
        node = null;
        var over = Mouse.DirectlyOver as DependencyObject;
        while (over is not null)
        {
            if (over is FrameworkElement fe)
            {
                var result = FindPortAndNode(fe, out node);
                if (result is not null) return result;
            }
            over = VisualTreeHelper.GetParent(over);
        }
        return null;
    }

    private void BlockNode_Delete_Click(object sender, RoutedEventArgs e)
    {
        var node = FindNodeFromContextMenu(sender);
        if (node is not null && Vm is not null)
            Vm.RemoveNode(node);
    }

    private static BlueprintNode? FindNodeFromContextMenu(object? sender)
    {
        var dep = sender as DependencyObject;
        while (dep is not null)
        {
            if (dep is ContextMenu cm && cm.PlacementTarget is FrameworkElement target)
                return target.DataContext as BlueprintNode;
            dep = VisualTreeHelper.GetParent(dep);
        }
        return null;
    }
}
