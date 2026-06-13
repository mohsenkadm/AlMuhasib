import 'package:flutter/material.dart';

import '../../core/constants/app_colors.dart';

class StickySummaryBar extends StatelessWidget {
  const StickySummaryBar({
    super.key,
    required this.label,
    required this.amount,
    this.secondaryLabel,
    this.secondaryAmount,
  });

  final String label;
  final String amount;
  final String? secondaryLabel;
  final String? secondaryAmount;

  @override
  Widget build(BuildContext context) {
    return Material(
      elevation: 8,
      color: Theme.of(context).colorScheme.surface,
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 12),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(label, style: Theme.of(context).textTheme.bodySmall),
                    Text(amount, style: Theme.of(context).textTheme.titleLarge),
                    if (secondaryLabel != null && secondaryAmount != null) ...[
                      const SizedBox(height: 4),
                      Text(
                        '$secondaryLabel: $secondaryAmount',
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

void showSuccessSnackbar(BuildContext context, String message) {
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      content: Row(
        children: [
          const Icon(Icons.check_circle, color: Colors.white),
          const SizedBox(width: 8),
          Expanded(child: Text(message)),
        ],
      ),
      backgroundColor: AppColors.success,
      behavior: SnackBarBehavior.floating,
    ),
  );
}

void showErrorSnackbar(BuildContext context, String message) {
  ScaffoldMessenger.of(context).showSnackBar(
    SnackBar(
      content: Text(message),
      backgroundColor: AppColors.error,
      behavior: SnackBarBehavior.floating,
    ),
  );
}
