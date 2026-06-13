import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';

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

class FilterChipOption {
  const FilterChipOption({required this.id, required this.label});

  final String id;
  final String label;
}

class _SearchFilterBarState extends State<SearchFilterBar> {
  final _controller = TextEditingController();
  String? _selectedFilter;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  void _onChanged(String value) {
    Future.delayed(const Duration(milliseconds: 350), () {
      if (_controller.text == value) {
        widget.onSearchChanged(value);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        TextField(
          controller: _controller,
          decoration: InputDecoration(
            hintText: widget.hint ?? 'search_hint'.tr(),
            prefixIcon: const Icon(Icons.search),
            suffixIcon: _controller.text.isNotEmpty
                ? IconButton(
                    icon: const Icon(Icons.clear),
                    onPressed: () {
                      _controller.clear();
                      widget.onSearchChanged('');
                      setState(() {});
                    },
                  )
                : null,
            border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
            filled: true,
          ),
          onChanged: (v) {
            setState(() {});
            _onChanged(v);
          },
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
