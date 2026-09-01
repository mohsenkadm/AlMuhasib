import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../models/master_data_models.dart';
import 'empty_search_state.dart';
import 'lookup_picker_controller.dart';

typedef LookupItemBuilder<T> = Widget Function(T item);

Future<T?> showLookupPickerSheet<T extends LookupItem>({
  required BuildContext context,
  required String title,
  required Future<List<T>> Function(String search) loadItems,
  LookupItemBuilder<T>? itemBuilder,
}) async {
  final tag = 'lookup_picker_${DateTime.now().microsecondsSinceEpoch}';
  Get.put(
    LookupPickerController<T>(loadItems: loadItems),
    tag: tag,
  );

  try {
    return await showModalBottomSheet<T>(
      context: context,
      isScrollControlled: true,
      builder: (ctx) => _LookupPickerSheet<T>(
        tag: tag,
        title: title,
        itemBuilder: itemBuilder,
      ),
    );
  } finally {
    Get.delete<LookupPickerController<T>>(tag: tag);
  }
}

class _LookupPickerSheet<T extends LookupItem> extends StatelessWidget {
  const _LookupPickerSheet({
    required this.tag,
    required this.title,
    this.itemBuilder,
  });

  final String tag;
  final String title;
  final LookupItemBuilder<T>? itemBuilder;

  LookupPickerController<T> get _controller =>
      Get.find<LookupPickerController<T>>(tag: tag);

  @override
  Widget build(BuildContext context) {
    final height = MediaQuery.of(context).size.height * 0.75;
    return SizedBox(
      height: height,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(title, style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 12),
            TextField(
              controller: _controller.searchController,
              decoration: InputDecoration(
                hintText: 'search_hint'.tr(),
                prefixIcon: const Icon(Icons.search),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
              ),
            ),
            const SizedBox(height: 12),
            Expanded(child: _buildBody(context)),
          ],
        ),
      ),
    );
  }

  Widget _buildBody(BuildContext context) {
    return Obx(() {
      if (_controller.loading.value) {
        return const Center(child: CircularProgressIndicator());
      }
      if (_controller.error.value != null) {
        return Center(child: Text(_controller.error.value.toString()));
      }
      if (_controller.items.isEmpty) {
        return EmptySearchState(onClear: _controller.clearSearch);
      }
      return ListView.separated(
        itemCount: _controller.items.length,
        separatorBuilder: (_, __) => const Divider(height: 1),
        itemBuilder: (context, index) {
          final item = _controller.items[index];
          if (itemBuilder != null) {
            return ListTile(
              onTap: () => Navigator.pop(context, item),
              title: itemBuilder!(item),
            );
          }
          return ListTile(
            title: Text(item.displayName),
            subtitle: Text([
              if (item.extra != null && item.extra!.isNotEmpty) item.extra!,
              if (item.balance != null)
                '${'balance'.tr()}: ${item.balance!.toStringAsFixed(0)}',
            ].where((e) => e.isNotEmpty).join(' • ')),
            onTap: () => Navigator.pop(context, item),
          );
        },
      );
    });
  }
}
