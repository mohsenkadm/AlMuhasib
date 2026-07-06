import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import 'search_filter_bar_controller.dart';

class SearchFilterBar extends StatelessWidget {
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
  Widget build(BuildContext context) {
    return GetBuilder<SearchFilterBarController>(
      init: SearchFilterBarController(onSearchChanged: onSearchChanged),
      autoRemove: true,
      builder: (controller) {
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Obx(
              () => TextField(
                controller: controller.searchController,
                decoration: InputDecoration(
                  hintText: hint ?? 'search_hint'.tr(),
                  prefixIcon: const Icon(Icons.search),
                  suffixIcon: controller.hasText.value
                      ? IconButton(
                          icon: const Icon(Icons.clear),
                          onPressed: controller.clear,
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
                onChanged: controller.onChanged,
              ),
            ),
            if (filterChips.isNotEmpty) ...[
              const SizedBox(height: 8),
              SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: Obx(
                  () => Row(
                    children: filterChips.map((chip) {
                      final selected = controller.selectedFilter.value == chip.id;
                      return Padding(
                        padding: const EdgeInsetsDirectional.only(end: 8),
                        child: FilterChip(
                          label: Text(chip.label),
                          selected: selected,
                          onSelected: (_) {
                            controller.toggleFilter(chip.id, selected);
                            onFilterSelected?.call(controller.selectedFilter.value);
                          },
                        ),
                      );
                    }).toList(),
                  ),
                ),
              ),
            ],
          ],
        );
      },
    );
  }
}

class FilterChipOption {
  const FilterChipOption({required this.id, required this.label});

  final String id;
  final String label;
}
