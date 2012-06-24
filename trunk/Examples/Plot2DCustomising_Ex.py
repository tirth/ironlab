﻿# Try running all at once, or running one line at a time to see graph update.

from math import *
from ironplot import * 

x = [0.1 * i for i in range(100)]
# Add a red line
subplot(1, 2, 0)
plot(x, [sin(xi) * xi **2 for xi in x], 'r')
subplot(1)
curve1 = plot(x, [sin(xi) * xi **2 for xi in x], 'r')
# curve1 is the line object, to get the plot object, we can use:
plot1 = currentplot()
# or we could also do plot1 = curve1.Plot
# Make legend visible and assign a name:
plot1.Legend.Visibility = Visibility.Visible
curve1.Title = "Test 1"
xlabel("Time [s]")
# or object-oriented label access:
plot1.LeftLabel.Text = "Displacement [m]"
# Set font properties for whole plot:
plot1.FontSize = 16
# Set curve properties:
curve1.StrokeThickness = 2
curve1.Stroke = Brushes.Blue
# Add another curve:
curve2 = Plot2DCurve(x, [sin(xi * 1.5) * xi**2 for xi in x])
curve2.Title = "Test 2"
plot1.Children.Add(curve2)
plot1.Axes.XAxes.Bottom.TickLength = -10
plot1.Axes.XAxes.Top.TickLength = 10
plot1.BackgroundPlot = Brushes.LightSteelBlue
plot1.FontStyle = FontStyles.Italic
plot1.FontWeight = FontWeights.SemiBold
curve1.QuickStrokeDash = QuickStrokeDash.Dash
# This specifies that there should be at most 20 ticks: if 20 ticks does not give a 'sensible'
# tick spacing then a smaller number is used
plot1.Axes.XAxes.Bottom.NumberOfTicks = plot1.Axes.XAxes.Top.NumberOfTicks = 20
plot1.LegendPosition = Position.Top
plot1.Legend.Orientation = Orientation.Horizontal