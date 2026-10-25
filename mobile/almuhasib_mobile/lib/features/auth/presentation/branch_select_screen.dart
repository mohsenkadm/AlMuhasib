import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../../../core/network/api_exception.dart';
import '../../../shared/models/auth_models.dart';

/// اختيار الفرع عند الدخول، أو تبديله لاحقاً من الإعدادات (`switchMode: true`).
class BranchSelectScreen extends StatefulWidget {
  const BranchSelectScreen({super.key, this.switchMode = false});

  final bool switchMode;

  @override
  State<BranchSelectScreen> createState() => _BranchSelectScreenState();
}

class _BranchSelectScreenState extends State<BranchSelectScreen> {
  final _loading = false.obs;
  final _refreshing = false.obs;
  final _error = RxnString();
  final _branches = <BranchInfo>[].obs;
  BranchInfo? _selected;

  @override
  void initState() {
    super.initState();
    _branches.assignAll(AppServices.prefs.allowedBranches);
    final currentId = AppServices.prefs.branchId;
    _selected = _branches.cast<BranchInfo?>().firstWhere(
          (b) => widget.switchMode
              ? b?.branchId == currentId
              : b?.isDefault == true,
          orElse: () => _branches.isNotEmpty ? _branches.first : null,
        );
    if (widget.switchMode) {
      _refreshBranches(silent: true);
    }
  }

  Future<void> _refreshBranches({bool silent = false}) async {
    if (!silent) _refreshing.value = true;
    _error.value = null;
    try {
      final response = await AppServices.auth.refreshBranches();
      _branches.assignAll(response.allowedBranches);
      if (_selected != null) {
        final match = _branches
            .where((b) => b.branchId == _selected!.branchId)
            .firstOrNull;
        if (match != null) _selected = match;
      } else if (_branches.isNotEmpty) {
        _selected = _branches.first;
      }
      setState(() {});
    } on ApiException catch (e) {
      if (!silent) _error.value = e.message;
    } catch (_) {
      if (!silent) _error.value = 'تعذر تحديث قائمة الفروع';
    } finally {
      _refreshing.value = false;
    }
  }

  Future<void> _confirm() async {
    if (_selected == null) {
      _error.value = 'اختر فرعاً للمتابعة';
      return;
    }
    _loading.value = true;
    _error.value = null;
    try {
      final previous = AppServices.prefs.branchId;
      final pendingOther = AppServices.offlineQueue.pending
          .where((w) => w.branchId != null && w.branchId != _selected!.branchId)
          .length;

      if (widget.switchMode &&
          previous != null &&
          previous != _selected!.branchId &&
          pendingOther > 0) {
        final proceed = await Get.dialog<bool>(
          AlertDialog(
            title: const Text('تبديل الفرع'),
            content: Text(
              'يوجد $pendingOther عملية معلّقة على الفرع الحالي. '
              'سيتم حذفها عند التبديل. هل تريد المتابعة؟',
            ),
            actions: [
              TextButton(
                onPressed: () => Get.back(result: false),
                child: const Text('إلغاء'),
              ),
              FilledButton(
                onPressed: () => Get.back(result: true),
                child: const Text('تبديل'),
              ),
            ],
          ),
        );
        if (proceed != true) return;
      }

      await AppServices.auth.selectBranch(_selected!.branchId);
      if (widget.switchMode) {
        Get.back(result: true);
      } else {
        Get.offAllNamed(AppServices.prefs.launchRoute);
      }
    } on ApiException catch (e) {
      _error.value = e.message;
    } catch (_) {
      _error.value = 'تعذر اختيار الفرع';
    } finally {
      _loading.value = false;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.switchMode ? 'تبديل الفرع' : 'اختر الفرع'),
        actions: [
          Obx(
            () => IconButton(
              tooltip: 'تحديث الفروع',
              onPressed: _refreshing.value ? null : () => _refreshBranches(),
              icon: _refreshing.value
                  ? const SizedBox(
                      width: 20,
                      height: 20,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.refresh),
            ),
          ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              widget.switchMode
                  ? 'اختر الفرع ثم أكّد. يمكنك تحديث القائمة بعد مزامنة Desktop.'
                  : 'يجب اختيار فرع قبل عرض بيانات النظام',
              style: Theme.of(context).textTheme.bodyMedium,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 16),
            Expanded(
              child: Obx(() {
                if (_branches.isEmpty) {
                  return Center(
                    child: Text(
                      'لا توجد فروع مسموحة. اسحب للتحديث أو تواصل مع الإدارة.',
                      textAlign: TextAlign.center,
                      style: Theme.of(context).textTheme.bodyMedium,
                    ),
                  );
                }
                return RefreshIndicator(
                  onRefresh: () => _refreshBranches(),
                  child: ListView.separated(
                    itemCount: _branches.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 8),
                    itemBuilder: (context, index) {
                      final branch = _branches[index];
                      final selected = _selected?.branchId == branch.branchId;
                      return ListTile(
                        selected: selected,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12),
                          side: BorderSide(
                            color: selected
                                ? Theme.of(context).colorScheme.primary
                                : Colors.black12,
                          ),
                        ),
                        title: Text(branch.name),
                        subtitle: Text(
                          [
                            branch.code,
                            if (branch.syncId != null &&
                                branch.syncId!.isNotEmpty)
                              'Sync',
                          ].join(' · '),
                        ),
                        trailing: selected
                            ? const Icon(Icons.check_circle)
                            : const Icon(Icons.circle_outlined),
                        onTap: () => setState(() => _selected = branch),
                      );
                    },
                  ),
                );
              }),
            ),
            Obx(() {
              if (_error.value == null) return const SizedBox.shrink();
              return Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: Text(
                  _error.value!,
                  style: TextStyle(color: Theme.of(context).colorScheme.error),
                  textAlign: TextAlign.center,
                ),
              );
            }),
            Obx(
              () => FilledButton(
                onPressed: _loading.value ? null : _confirm,
                child: _loading.value
                    ? const SizedBox(
                        width: 22,
                        height: 22,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Text(widget.switchMode ? 'تأكيد التبديل' : 'متابعة'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
