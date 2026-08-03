import 'package:easy_localization/easy_localization.dart';
import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';

import '../../../core/constants/app_colors.dart';
import '../../utils/formatters.dart';

class AppChartSeries {
  const AppChartSeries({
    required this.label,
    required this.values,
    required this.color,
  });

  final String label;
  final List<double> values;
  final Color color;
}

class AppChartCard extends StatelessWidget {
  const AppChartCard({
    super.key,
    required this.title,
    required this.child,
    this.height = 220,
    this.legend,
    this.subtitle,
  });

  final String title;
  final Widget child;
  final double height;
  final Widget? legend;
  final String? subtitle;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 16, 16, 12),
      decoration: BoxDecoration(
        color: isDark ? AppColors.surfaceDarkCard : Colors.white,
        borderRadius: BorderRadius.circular(20),
        border: Border.all(
          color: isDark
              ? Colors.white.withValues(alpha: 0.06)
              : AppColors.primary.withValues(alpha: 0.06),
        ),
        boxShadow: AppColors.cardShadow(dark: isDark),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            title,
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
          ),
          if (subtitle != null) ...[
            const SizedBox(height: 4),
            Text(
              subtitle!,
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                    color: Theme.of(context).colorScheme.onSurfaceVariant,
                  ),
            ),
          ],
          if (legend != null) ...[
            const SizedBox(height: 10),
            legend!,
          ],
          const SizedBox(height: 14),
          SizedBox(height: height, child: child),
        ],
      ),
    );
  }
}

class AppChartLegend extends StatelessWidget {
  const AppChartLegend({super.key, required this.items});

  final List<(String label, Color color)> items;

  @override
  Widget build(BuildContext context) {
    return Wrap(
      spacing: 14,
      runSpacing: 6,
      children: [
        for (final item in items)
          Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 10,
                height: 10,
                decoration: BoxDecoration(
                  color: item.$2,
                  borderRadius: BorderRadius.circular(3),
                ),
              ),
              const SizedBox(width: 6),
              Text(
                item.$1,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      fontWeight: FontWeight.w600,
                    ),
              ),
            ],
          ),
      ],
    );
  }
}

class AppGroupedBarChart extends StatelessWidget {
  const AppGroupedBarChart({
    super.key,
    required this.labels,
    required this.series,
    this.valueAsCurrency = false,
  });

  final List<String> labels;
  final List<AppChartSeries> series;
  final bool valueAsCurrency;

  @override
  Widget build(BuildContext context) {
    if (labels.isEmpty || series.every((s) => s.values.every((v) => v == 0))) {
      return Center(child: Text('no_data'.tr()));
    }

    final barCount = series.length;
    final groupWidth = barCount == 1 ? 18.0 : 12.0;
    final maxY = series
        .expand((s) => s.values)
        .fold<double>(0, (m, v) => v > m ? v : m);
    final paddedMax = maxY <= 0 ? 1.0 : maxY * 1.2;

    return BarChart(
      BarChartData(
        maxY: paddedMax,
        gridData: FlGridData(
          show: true,
          drawVerticalLine: false,
          getDrawingHorizontalLine: (v) => FlLine(
            color: Theme.of(context).dividerColor.withValues(alpha: 0.12),
            strokeWidth: 1,
          ),
        ),
        borderData: FlBorderData(show: false),
        titlesData: FlTitlesData(
          topTitles: const AxisTitles(),
          rightTitles: const AxisTitles(),
          leftTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: true,
              reservedSize: 36,
              getTitlesWidget: (value, _) => Text(
                _compact(value),
                style: const TextStyle(fontSize: 10),
              ),
            ),
          ),
          bottomTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: true,
              reservedSize: 28,
              getTitlesWidget: (value, _) {
                final i = value.toInt();
                if (i < 0 || i >= labels.length) {
                  return const SizedBox.shrink();
                }
                final label = labels[i];
                final short = label.length > 7 ? label.substring(5) : label;
                return Padding(
                  padding: const EdgeInsets.only(top: 6),
                  child: Text(short, style: const TextStyle(fontSize: 10)),
                );
              },
            ),
          ),
        ),
        barGroups: [
          for (var i = 0; i < labels.length; i++)
            BarChartGroupData(
              x: i,
              barsSpace: 4,
              barRods: [
                for (var s = 0; s < series.length; s++)
                  BarChartRodData(
                    toY: i < series[s].values.length ? series[s].values[i] : 0,
                    color: series[s].color,
                    width: groupWidth,
                    borderRadius: const BorderRadius.vertical(
                      top: Radius.circular(6),
                    ),
                  ),
              ],
            ),
        ],
        barTouchData: BarTouchData(
          touchTooltipData: BarTouchTooltipData(
            getTooltipItem: (group, groupIndex, rod, rodIndex) {
              final seriesLabel =
                  rodIndex < series.length ? series[rodIndex].label : '';
              final value = valueAsCurrency
                  ? formatCurrency(rod.toY)
                  : rod.toY.toStringAsFixed(0);
              return BarTooltipItem(
                '$seriesLabel\n$value',
                const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.w600,
                  fontSize: 12,
                ),
              );
            },
          ),
        ),
      ),
    );
  }

  String _compact(double value) {
    if (value >= 1000000) return '${(value / 1000000).toStringAsFixed(1)}M';
    if (value >= 1000) return '${(value / 1000).toStringAsFixed(0)}k';
    return value.toInt().toString();
  }
}

class AppHorizontalBarChart extends StatelessWidget {
  const AppHorizontalBarChart({
    super.key,
    required this.points,
    this.color = AppColors.primary,
    this.valueAsCurrency = false,
  });

  final List<(String label, double value)> points;
  final Color color;
  final bool valueAsCurrency;

  @override
  Widget build(BuildContext context) {
    if (points.isEmpty) {
      return Center(child: Text('no_data'.tr()));
    }

    final maxV = points.fold<double>(0, (m, p) => p.$2 > m ? p.$2 : m);
    final safeMax = maxV <= 0 ? 1.0 : maxV;

    return ListView.separated(
      physics: const NeverScrollableScrollPhysics(),
      shrinkWrap: true,
      itemCount: points.length,
      separatorBuilder: (_, __) => const SizedBox(height: 10),
      itemBuilder: (context, index) {
        final point = points[index];
        final ratio = (point.$2 / safeMax).clamp(0.0, 1.0);
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    point.$1,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: Theme.of(context).textTheme.bodySmall?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                  ),
                ),
                Text(
                  valueAsCurrency
                      ? formatCurrency(point.$2)
                      : point.$2.toStringAsFixed(0),
                  style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        fontWeight: FontWeight.w800,
                      ),
                ),
              ],
            ),
            const SizedBox(height: 6),
            ClipRRect(
              borderRadius: BorderRadius.circular(99),
              child: LinearProgressIndicator(
                value: ratio,
                minHeight: 8,
                backgroundColor: color.withValues(alpha: 0.12),
                color: color,
              ),
            ),
          ],
        );
      },
    );
  }
}

class AppDonutChart extends StatelessWidget {
  const AppDonutChart({
    super.key,
    required this.sections,
    this.centerLabel,
    this.centerValue,
    this.valueAsCurrency = false,
  });

  final List<(String label, double value, Color color)> sections;
  final String? centerLabel;
  final String? centerValue;
  final bool valueAsCurrency;

  @override
  Widget build(BuildContext context) {
    final total = sections.fold<double>(0, (s, e) => s + e.$2);
    if (total <= 0) {
      return Center(child: Text('no_data'.tr()));
    }

    return Row(
      children: [
        Expanded(
          flex: 5,
          child: Stack(
            alignment: Alignment.center,
            children: [
              PieChart(
                PieChartData(
                  sectionsSpace: 3,
                  centerSpaceRadius: 42,
                  sections: [
                    for (final s in sections)
                      PieChartSectionData(
                        value: s.$2,
                        color: s.$3,
                        radius: 28,
                        showTitle: false,
                      ),
                  ],
                ),
              ),
              if (centerLabel != null || centerValue != null)
                Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    if (centerValue != null)
                      Text(
                        centerValue!,
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                              fontWeight: FontWeight.w800,
                            ),
                      ),
                    if (centerLabel != null)
                      Text(
                        centerLabel!,
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                  ],
                ),
            ],
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          flex: 5,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              for (final s in sections) ...[
                Row(
                  children: [
                    Container(
                      width: 10,
                      height: 10,
                      decoration: BoxDecoration(
                        color: s.$3,
                        borderRadius: BorderRadius.circular(3),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        s.$1,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ),
                    Text(
                      valueAsCurrency
                          ? formatCurrency(s.$2)
                          : s.$2.toStringAsFixed(0),
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                            fontWeight: FontWeight.w800,
                          ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

class AppLineChart extends StatelessWidget {
  const AppLineChart({
    super.key,
    required this.values,
    this.labels = const [],
    this.color = AppColors.primary,
    this.valueAsCurrency = true,
  });

  final List<double> values;
  final List<String> labels;
  final Color color;
  final bool valueAsCurrency;

  @override
  Widget build(BuildContext context) {
    if (values.isEmpty || values.every((v) => v == 0)) {
      return Center(child: Text('no_data'.tr()));
    }

    final spots = [
      for (var i = 0; i < values.length; i++) FlSpot(i.toDouble(), values[i]),
    ];

    return LineChart(
      LineChartData(
        gridData: FlGridData(
          show: true,
          drawVerticalLine: false,
          getDrawingHorizontalLine: (v) => FlLine(
            color: Theme.of(context).dividerColor.withValues(alpha: 0.12),
            strokeWidth: 1,
          ),
        ),
        titlesData: FlTitlesData(
          topTitles: const AxisTitles(),
          rightTitles: const AxisTitles(),
          leftTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: true,
              reservedSize: 42,
              getTitlesWidget: (value, _) => Text(
                _compact(value),
                style: const TextStyle(fontSize: 10),
              ),
            ),
          ),
          bottomTitles: AxisTitles(
            sideTitles: SideTitles(
              showTitles: labels.isNotEmpty,
              interval: (values.length / 4).clamp(1, 7).toDouble(),
              getTitlesWidget: (value, _) {
                final index = value.toInt();
                if (index < 0 || index >= labels.length) {
                  return const SizedBox.shrink();
                }
                final label = labels[index];
                final short =
                    label.length > 8 ? label.substring(label.length - 5) : label;
                return Padding(
                  padding: const EdgeInsets.only(top: 6),
                  child: Text(short, style: const TextStyle(fontSize: 10)),
                );
              },
            ),
          ),
        ),
        borderData: FlBorderData(show: false),
        lineBarsData: [
          LineChartBarData(
            spots: spots,
            isCurved: true,
            color: color,
            barWidth: 3.5,
            isStrokeCapRound: true,
            dotData: FlDotData(
              show: values.length <= 16,
              getDotPainter: (spot, percent, bar, index) => FlDotCirclePainter(
                radius: 3.5,
                color: Colors.white,
                strokeWidth: 2.5,
                strokeColor: color,
              ),
            ),
            belowBarData: BarAreaData(
              show: true,
              gradient: LinearGradient(
                begin: Alignment.topCenter,
                end: Alignment.bottomCenter,
                colors: [
                  color.withValues(alpha: 0.28),
                  color.withValues(alpha: 0.02),
                ],
              ),
            ),
          ),
        ],
        lineTouchData: LineTouchData(
          handleBuiltInTouches: true,
          touchTooltipData: LineTouchTooltipData(
            getTooltipItems: (touched) => touched
                .map(
                  (spot) => LineTooltipItem(
                    valueAsCurrency
                        ? formatCurrency(spot.y)
                        : spot.y.toStringAsFixed(0),
                    const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.w600,
                      fontSize: 12,
                    ),
                  ),
                )
                .toList(),
          ),
        ),
      ),
    );
  }

  String _compact(double value) {
    if (value >= 1000000) return '${(value / 1000000).toStringAsFixed(1)}M';
    if (value >= 1000) return '${(value / 1000).toStringAsFixed(0)}k';
    return value.toInt().toString();
  }
}

/// Aligns two named count series onto a shared sorted label axis.
(List<String> labels, List<double> a, List<double> b) alignNamedSeries(
  List<(String name, double value)> left,
  List<(String name, double value)> right,
) {
  final labels = <String>{
    ...left.map((e) => e.$1),
    ...right.map((e) => e.$1),
  }.toList()
    ..sort();
  final leftMap = {for (final e in left) e.$1: e.$2};
  final rightMap = {for (final e in right) e.$1: e.$2};
  return (
    labels,
    [for (final l in labels) leftMap[l] ?? 0],
    [for (final l in labels) rightMap[l] ?? 0],
  );
}
