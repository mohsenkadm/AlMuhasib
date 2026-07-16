import 'dart:async';

import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';

import '../../core/constants/app_colors.dart';

class SearchFilterBar extends StatefulWidget {
  const SearchFilterBar({
    super.key,
    required this.onSearchChanged,
    this.hint,
    this.filterChips = const [],
    this.onFilterSelected,
  });

  final ValueChanged<String> onSearchChanged;
  final String? hint;
  final List<FilterChipOption> filterChips;
  final ValueChanged<String?>? onFilterSelected;

  @override
  State<SearchFilterBar> createState() => _SearchFilterBarState();
}

class _SearchFilterBarState extends State<SearchFilterBar> {
  final _searchController = TextEditingController();
  Timer? _debounce;
  String? _selectedFilter;

  @override
  void dispose() {
    _debounce?.cancel();
    _searchController.dispose();
    super.dispose();
  }

  void _onChanged(String value) {
    setState(() {});
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 350), () {
      widget.onSearchChanged(value);
    });
  }

  void _clear() {
    _debounce?.cancel();
    _searchController.clear();
    setState(() {});
    widget.onSearchChanged('');
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      mainAxisSize: MainAxisSize.min,
      children: [
        TextField(
          controller: _searchController,
          decoration: InputDecoration(
            hintText: widget.hint ?? 'search_hint'.tr(),
            prefixIcon: const Icon(Icons.search),
            suffixIcon: _searchController.text.isNotEmpty
                ? IconButton(
                    icon: const Icon(Icons.clear),
                    onPressed: _clear,
                  )
                : null,
            border: OutlineInputBorder(
              borderRadius: BorderRadius.circular(14),
              borderSide: BorderSide.none,
            ),
            filled: true,
            fillColor: Theme.of(context)
                .colorScheme
                .surfaceContainerHighest
                .withValues(alpha: 0.5),
          ),
          onChanged: _onChanged,
        ),
        if (widget.filterChips.isNotEmpty) ...[
          const SizedBox(height: 8),
          SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: widget.filterChips.map((chip) {
                final selected = _selectedFilter == chip.id;
                return Padding(
                  padding: const EdgeInsetsDirectional.only(end: 8),
                  child: FilterChip(
                    label: Text(chip.label),
                    selected: selected,
                    showCheckmark: false,
                    selectedColor: Theme.of(context).colorScheme.primary,
                    labelStyle: TextStyle(
                      color: selected
                          ? Colors.white
                          : Theme.of(context).colorScheme.onSurface,
                      fontWeight: FontWeight.w700,
                    ),
                    backgroundColor: Theme.of(context).brightness == Brightness.dark
                        ? AppColors.surfaceDarkCard
                        : Colors.white,
                    side: BorderSide(
                      color: selected
                          ? Colors.transparent
                          : Colors.black.withValues(alpha: 0.06),
                    ),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(999),
                    ),
                    onSelected: (_) {
                      setState(() {
                        _selectedFilter = selected ? null : chip.id;
                      });
                      widget.onFilterSelected?.call(_selectedFilter);
                    },
                  ),
                );
              }).toList(),
            ),
          ),
        ],
      ],
    );
  }
}

class FilterChipOption {
  const FilterChipOption({required this.id, required this.label});

  final String id;
  final String label;
}
