import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';

class EmptySearchState extends StatelessWidget {
  const EmptySearchState({super.key, this.onClear});

  final VoidCallback? onClear;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.search_off, size: 48, color: Theme.of(context).disabledColor),
          const SizedBox(height: 12),
          Text('no_search_results'.tr()),
          if (onClear != null) ...[
            const SizedBox(height: 12),
            TextButton(onPressed: onClear, child: Text('clear_search'.tr())),
          ],
        ],
      ),
    );
  }
}
