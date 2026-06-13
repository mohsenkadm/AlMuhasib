import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';

import '../models/master_data_models.dart';
import 'empty_search_state.dart';

typedef LookupItemBuilder<T> = Widget Function(T item);

Future<T?> showLookupPickerSheet<T extends LookupItem>({
  required BuildContext context,
  required String title,
  required Future<List<T>> Function(String search) loadItems,
  LookupItemBuilder<T>? itemBuilder,
}) async {
  return showModalBottomSheet<T>(
    context: context,
    isScrollControlled: true,
    builder: (ctx) => _LookupPickerSheet<T>(
      title: title,
      loadItems: loadItems,
      itemBuilder: itemBuilder,
    ),
  );
}

class _LookupPickerSheet<T extends LookupItem> extends StatefulWidget {
  const _LookupPickerSheet({
    required this.title,
    required this.loadItems,
    this.itemBuilder,
  });

  final String title;
  final Future<List<T>> Function(String search) loadItems;
  final LookupItemBuilder<T>? itemBuilder;

  @override
  State<_LookupPickerSheet<T>> createState() => _LookupPickerSheetState<T>();
}

class _LookupPickerSheetState<T extends LookupItem>
    extends State<_LookupPickerSheet<T>> {
  final _searchController = TextEditingController();
  List<T> _items = [];
  bool _loading = true;
  Object? _error;

  @override
  void initState() {
    super.initState();
    _load('');
    _searchController.addListener(() => _load(_searchController.text));
  }

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  Future<void> _load(String search) async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final items = await widget.loadItems(search);
      if (mounted) setState(() => _items = items);
    } catch (e) {
      if (mounted) setState(() => _error = e);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

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
            Text(widget.title, style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: 12),
            TextField(
              controller: _searchController,
              decoration: InputDecoration(
                hintText: 'search_hint'.tr(),
                prefixIcon: const Icon(Icons.search),
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
              ),
            ),
            const SizedBox(height: 12),
            Expanded(child: _buildBody()),
          ],
        ),
      ),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null) {
      return Center(child: Text(_error.toString()));
    }
    if (_items.isEmpty) {
      return EmptySearchState(onClear: () {
        _searchController.clear();
        _load('');
      });
    }
    return ListView.separated(
      itemCount: _items.length,
      separatorBuilder: (_, __) => const Divider(height: 1),
      itemBuilder: (context, index) {
        final item = _items[index];
        if (widget.itemBuilder != null) {
          return ListTile(
            onTap: () => Navigator.pop(context, item),
            title: widget.itemBuilder!(item),
          );
        }
        return ListTile(
          title: Text(item.name),
          subtitle: item.extra != null ? Text(item.extra!) : null,
          onTap: () => Navigator.pop(context, item),
        );
      },
    );
  }
}
