import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/theme/app_spacing.dart';
import '../common_widgets.dart';
import '../search_filter_bar.dart';
import '../shimmer_widgets.dart';
import 'app_async_body.dart';
import 'app_sliver_app_bar.dart';

class AppListPage<T> extends StatelessWidget {
  const AppListPage({
    super.key,
    required this.title,
    this.subtitle,
    this.isLoading,
    this.error,
    this.items,
    this.staticItems,
    required this.itemBuilder,
    this.onRefresh,
    this.onRetry,
    this.searchController,
    this.onSearch,
    this.onSearchChanged,
    this.fabLabel,
    this.fabIcon = Icons.add_rounded,
    this.onFab,
    this.emptyMessage,
    this.emptyIcon,
    this.actions,
    this.padding,
    this.useSearchFilterBar = true,
    this.filterPanel,
  });

  final String title;
  final String? subtitle;
  final RxBool? isLoading;
  final Rxn<Object>? error;
  final RxList<T>? items;
  final List<T>? staticItems;
  final Widget Function(BuildContext context, T item, int index) itemBuilder;
  final Future<void> Function()? onRefresh;
  final VoidCallback? onRetry;
  final TextEditingController? searchController;
  final VoidCallback? onSearch;
  final ValueChanged<String>? onSearchChanged;
  final String? fabLabel;
  final IconData fabIcon;
  final VoidCallback? onFab;
  final String? emptyMessage;
  final IconData? emptyIcon;
  final List<Widget>? actions;
  final EdgeInsets? padding;
  final bool useSearchFilterBar;
  final Widget? filterPanel;

  List<T> get _resolvedItems => staticItems ?? items?.toList() ?? [];

  @override
  Widget build(BuildContext context) {
    Widget buildList(List<T> data) {
      if (data.isEmpty) {
        return ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          children: [
            SizedBox(
              height: MediaQuery.sizeOf(context).height * 0.45,
              child: EmptyStateWidget(
                message: emptyMessage,
                icon: emptyIcon,
                onRetry: onRetry,
              ),
            ),
          ],
        );
      }

      return ListView.builder(
        physics: const AlwaysScrollableScrollPhysics(),
        padding: padding ??
            const EdgeInsets.fromLTRB(
              AppSpacing.xl,
              AppSpacing.md,
              AppSpacing.xl,
              120,
            ),
        itemCount: data.length,
        itemBuilder: (context, index) => Padding(
          padding: const EdgeInsets.only(bottom: AppSpacing.md),
          child: itemBuilder(context, data[index], index),
        ),
      );
    }

    Widget content(bool loading, Object? err, List<T> data) {
      return AppAsyncBody<List<T>>(
        isLoading: loading,
        error: err,
        data: data,
        onRetry: onRetry,
        loadingWidget: const ListShimmer(),
        emptyMessage: emptyMessage,
        showEmptyWhenNull: false,
        builder: (context, list) => buildList(list),
      );
    }

    final listBody = (isLoading != null && error != null && items != null)
        ? Obx(() => content(
              isLoading!.value,
              error!.value,
              items!.toList(),
            ))
        : content(
            false,
            null,
            _resolvedItems,
          );

    return Scaffold(
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          AppStandardAppBar(
            title: title,
            subtitle: subtitle,
            actions: [
              ...?actions,
              if (onFab != null)
                IconButton(
                  tooltip: fabLabel,
                  onPressed: onFab,
                  icon: Icon(fabIcon),
                ),
            ],
          ),
          if (filterPanel != null)
            filterPanel!
          else if (searchController != null || onSearchChanged != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AppSpacing.lg,
                AppSpacing.sm,
                AppSpacing.lg,
                0,
              ),
              child: useSearchFilterBar && onSearchChanged != null
                  ? SearchFilterBar(onSearchChanged: onSearchChanged!)
                  : TextField(
                      controller: searchController,
                      decoration: InputDecoration(
                        hintText: 'search'.tr(),
                        prefixIcon: const Icon(Icons.search_rounded),
                        suffixIcon: onSearch != null
                            ? IconButton(
                                icon: const Icon(Icons.refresh_rounded),
                                onPressed: onSearch,
                              )
                            : null,
                      ),
                      onSubmitted: onSearch != null ? (_) => onSearch!() : null,
                    ),
            ),
          Expanded(
            child: onRefresh != null
                ? RefreshIndicator(
                    onRefresh: onRefresh!,
                    child: listBody,
                  )
                : listBody,
          ),
        ],
      ),
    );
  }
}
