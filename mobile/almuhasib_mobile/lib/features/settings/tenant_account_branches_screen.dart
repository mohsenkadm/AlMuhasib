import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../core/constants/app_colors.dart';
import '../../core/getx/app_services.dart';
import '../../shared/models/auth_models.dart';
import '../../shared/widgets/common_widgets.dart';
import '../../shared/widgets/design_system/design_system.dart';

class TenantAccountBranchesController extends GetxController {
  final isLoading = true.obs;
  final saving = false.obs;
  final Rxn<Object> error = Rxn<Object>();
  final accounts = <TenantAccountListItem>[].obs;
  final catalog = <BranchInfo>[].obs;
  final selectedAccount = Rxn<TenantAccountListItem>();
  final selectedBranchIds = <int>{}.obs;
  final defaultBranchId = Rxn<int>();

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      final list = await AppServices.auth.listTenantAccounts();
      accounts.assignAll(list);
      final allBranches = await AppServices.auth.listAllTenantBranches();
      catalog.assignAll(allBranches);
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }

  void _mergeCatalog(Iterable<BranchInfo> branches) {
    final byId = {for (final b in catalog) b.branchId: b};
    for (final b in branches) {
      byId[b.branchId] = b;
    }
    catalog.assignAll(
      byId.values.toList()
        ..sort((a, b) {
          if (a.isMain != b.isMain) return a.isMain ? -1 : 1;
          return a.name.compareTo(b.name);
        }),
    );
  }

  Future<void> openAccount(TenantAccountListItem account) async {
    selectedAccount.value = account;
    try {
      final response =
          await AppServices.auth.getTenantAccountBranches(account.id);
      _mergeCatalog(response.branches);
      selectedBranchIds
        ..clear()
        ..addAll(response.branches.map((b) => b.branchId));
      defaultBranchId.value = response.defaultBranchId ??
          (response.branches.length == 1
              ? response.branches.first.branchId
              : null);
    } catch (e) {
      AppExceptionHandler.showError(e);
    }
  }

  void clearSelection() {
    selectedAccount.value = null;
    selectedBranchIds.clear();
    defaultBranchId.value = null;
  }

  void toggleBranch(int branchId, bool selected) {
    if (selected) {
      selectedBranchIds.add(branchId);
      defaultBranchId.value ??= branchId;
    } else {
      selectedBranchIds.remove(branchId);
      if (defaultBranchId.value == branchId) {
        defaultBranchId.value =
            selectedBranchIds.isEmpty ? null : selectedBranchIds.first;
      }
    }
    selectedBranchIds.refresh();
  }

  Future<void> save() async {
    final account = selectedAccount.value;
    if (account == null) return;
    if (selectedBranchIds.isEmpty) {
      AppExceptionHandler.showError('branches_required'.tr());
      return;
    }
    saving.value = true;
    try {
      await AppServices.auth.setTenantAccountBranches(
        accountId: account.id,
        branchIds: selectedBranchIds.toList(),
        defaultBranchId: defaultBranchId.value,
      );
      AppExceptionHandler.showSuccess('settings_saved'.tr());

      final currentUser = AppServices.prefs.username;
      if (currentUser != null &&
          currentUser.toLowerCase() == account.username.toLowerCase()) {
        await AppServices.auth.refreshBranches();
      }
      clearSelection();
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      saving.value = false;
    }
  }
}

class TenantAccountBranchesScreen
    extends GetView<TenantAccountBranchesController> {
  const TenantAccountBranchesScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Obx(() {
      final editing = controller.selectedAccount.value;
      return AppPageScaffold(
        title: editing == null
            ? 'account_branches'.tr()
            : editing.username,
        leading: editing == null
            ? null
            : IconButton(
                icon: const Icon(Icons.arrow_back),
                onPressed: controller.clearSelection,
              ),
        actions: [
          if (editing != null)
            IconButton(
              icon: controller.saving.value
                  ? const SizedBox(
                      width: 18,
                      height: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : const Icon(Icons.save_outlined),
              onPressed: controller.saving.value ? null : controller.save,
            ),
        ],
        body: () {
          if (controller.isLoading.value) {
            return const Center(child: CircularProgressIndicator());
          }
          if (controller.error.value != null) {
            return ErrorStateWidget(
              message: AppExceptionHandler.messageFor(controller.error.value),
              onRetry: controller.load,
            );
          }
          if (editing == null) {
            return RefreshIndicator(
              onRefresh: controller.load,
              child: ListView.separated(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 120),
                itemCount: controller.accounts.length,
                separatorBuilder: (_, __) => const SizedBox(height: 8),
                itemBuilder: (context, index) {
                  final account = controller.accounts[index];
                  return ListTile(
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12),
                    ),
                    tileColor: Theme.of(context)
                        .colorScheme
                        .surfaceContainerHighest
                        .withValues(alpha: 0.35),
                    leading: CircleAvatar(
                      backgroundColor:
                          AppColors.primary.withValues(alpha: 0.12),
                      child: Icon(
                        account.isOwner
                            ? Icons.star_outline
                            : Icons.person_outline,
                        color: AppColors.primary,
                      ),
                    ),
                    title: Text(account.username),
                    subtitle: Text(
                      [
                        if (account.isOwner) 'owner'.tr(),
                        account.isActive ? 'active'.tr() : 'inactive'.tr(),
                      ].join(' · '),
                    ),
                    trailing: const Icon(Icons.chevron_left),
                    onTap: () => controller.openAccount(account),
                  );
                },
              ),
            );
          }
          return ListView(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 120),
            children: [
              Text(
                'select_branches'.tr(),
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.w800,
                    ),
              ),
              const SizedBox(height: 8),
              ...controller.catalog.map((branch) {
                final checked =
                    controller.selectedBranchIds.contains(branch.branchId);
                final isDefault =
                    controller.defaultBranchId.value == branch.branchId;
                return CheckboxListTile(
                  value: checked,
                  onChanged: (v) =>
                      controller.toggleBranch(branch.branchId, v ?? false),
                  title: Text(branch.name),
                  subtitle: Text(
                    [
                      if (branch.code.isNotEmpty) branch.code,
                      if (branch.isMain) 'main_branch'.tr(),
                      if (isDefault) 'default_branch'.tr(),
                    ].where((s) => s.isNotEmpty).join(' · '),
                  ),
                  secondary: IconButton(
                    tooltip: 'set_default_branch'.tr(),
                    icon: Icon(
                      isDefault ? Icons.flag : Icons.flag_outlined,
                      color: isDefault ? AppColors.primary : null,
                    ),
                    onPressed: checked
                        ? () =>
                            controller.defaultBranchId.value = branch.branchId
                        : null,
                  ),
                );
              }),
            ],
          );
        }(),
      );
    });
  }
}
