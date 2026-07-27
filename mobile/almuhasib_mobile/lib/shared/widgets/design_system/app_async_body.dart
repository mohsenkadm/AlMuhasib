import 'package:flutter/material.dart';

import '../common_widgets.dart';
import '../shimmer_widgets.dart';
import 'app_exception_handler.dart';

typedef AppAsyncBuilder<T> = Widget Function(BuildContext context, T data);

class AppAsyncBody<T> extends StatelessWidget {
  const AppAsyncBody({
    super.key,
    required this.isLoading,
    required this.error,
    required this.data,
    required this.builder,
    this.onRetry,
    this.loadingWidget,
    this.emptyMessage,
    this.showEmptyWhenNull = true,
  });

  final bool isLoading;
  final Object? error;
  final T? data;
  final AppAsyncBuilder<T> builder;
  final VoidCallback? onRetry;
  final Widget? loadingWidget;
  final String? emptyMessage;
  final bool showEmptyWhenNull;

  @override
  Widget build(BuildContext context) {
    if (isLoading) {
      return loadingWidget ?? const ListShimmer();
    }
    if (error != null) {
      return ErrorStateWidget(
        message: AppExceptionHandler.messageFor(error),
        onRetry: onRetry,
      );
    }
    if (data == null && showEmptyWhenNull) {
      return EmptyStateWidget(message: emptyMessage, onRetry: onRetry);
    }
    return builder(context, data as T);
  }
}
