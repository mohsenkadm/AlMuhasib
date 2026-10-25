import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../core/getx/app_services.dart';
import '../../../shared/models/auth_models.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';

class ExchangeRatesController extends GetxController {
  final isLoading = true.obs;
  final saving = false.obs;
  final Rxn<Object> error = Rxn<Object>();
  final items = <LatestExchangeRateResponse>[].obs;
  final rateController = TextEditingController();
  final notesController = TextEditingController();
  final rateDate = DateTime.now().obs;
  final formKey = GlobalKey<FormState>();

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = null;
    try {
      final list = await AppServices.auth.listExchangeRates();
      items.assignAll(list);
      if (rateController.text.trim().isEmpty && list.isNotEmpty) {
        rateController.text = list.first.usdToIqd.toString();
      }
    } catch (e) {
      error.value = e;
    } finally {
      isLoading.value = false;
    }
  }

  Future<void> pickDate(BuildContext context) async {
    final picked = await showDatePicker(
      context: context,
      initialDate: rateDate.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (picked != null) rateDate.value = picked;
  }

  Future<void> save() async {
    if (!formKey.currentState!.validate()) return;
    final rate = double.tryParse(rateController.text.trim());
    if (rate == null || rate <= 0) {
      AppExceptionHandler.showError('fx_rate_required'.tr());
      return;
    }
    saving.value = true;
    try {
      await AppServices.auth.createExchangeRate(
        CreateExchangeRateRequest(
          rateDate: rateDate.value,
          usdToIqd: rate,
          notes: notesController.text.trim().isEmpty
              ? null
              : notesController.text.trim(),
        ),
      );
      AppExceptionHandler.showSuccess('settings_saved'.tr());
      notesController.clear();
      await load();
    } catch (e) {
      AppExceptionHandler.showError(e);
    } finally {
      saving.value = false;
    }
  }

  @override
  void onClose() {
    rateController.dispose();
    notesController.dispose();
    super.onClose();
  }
}

class ExchangeRatesScreen extends GetView<ExchangeRatesController> {
  const ExchangeRatesScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: 'exchange_rates'.tr(),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 12, 20, 8),
            child: Form(
              key: controller.formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'add_exchange_rate'.tr(),
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.w800,
                        ),
                  ),
                  const SizedBox(height: 12),
                  Obx(
                    () => ListTile(
                      contentPadding: EdgeInsets.zero,
                      leading: const Icon(Icons.calendar_today_outlined),
                      title: Text(formatDate(controller.rateDate.value)),
                      trailing: TextButton(
                        onPressed: () => controller.pickDate(context),
                        child: Text('date'.tr()),
                      ),
                    ),
                  ),
                  AppTextField(
                    controller: controller.rateController,
                    label: 'fx_rate'.tr(),
                    prefixIcon: Icons.currency_exchange,
                    keyboardType:
                        const TextInputType.numberWithOptions(decimal: true),
                    validator: (v) {
                      final n = double.tryParse(v?.trim() ?? '');
                      if (n == null || n <= 0) return 'fx_rate_required'.tr();
                      return null;
                    },
                  ),
                  const SizedBox(height: 8),
                  AppTextField(
                    controller: controller.notesController,
                    label: 'notes'.tr(),
                    prefixIcon: Icons.notes_outlined,
                    maxLines: 2,
                  ),
                  const SizedBox(height: 12),
                  Obx(
                    () => FilledButton.icon(
                      onPressed:
                          controller.saving.value ? null : controller.save,
                      icon: controller.saving.value
                          ? const SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Icon(Icons.save_outlined),
                      label: Text('save'.tr()),
                    ),
                  ),
                ],
              ),
            ),
          ),
          const Divider(height: 24),
          Expanded(
            child: Obx(() {
              if (controller.isLoading.value) {
                return const Center(child: CircularProgressIndicator());
              }
              if (controller.error.value != null) {
                return ErrorStateWidget(
                  message: AppExceptionHandler.messageFor(
                    controller.error.value,
                  ),
                  onRetry: controller.load,
                );
              }
              if (controller.items.isEmpty) {
                return Center(
                  child: EmptyStateWidget(onRetry: controller.load),
                );
              }
              final items = controller.items.toList(growable: false);
              return RefreshIndicator(
                onRefresh: controller.load,
                child: ListView.separated(
                  padding: const EdgeInsets.fromLTRB(20, 0, 20, 120),
                  itemCount: items.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 10),
                  itemBuilder: (context, index) {
                    final item = items[index];
                    return AppEntityCard(
                      title: formatCurrency(item.usdToIqd),
                      subtitle: [
                        formatDate(item.rateDate),
                        if (item.notes.isNotEmpty) item.notes,
                      ].join(' • '),
                      leading: const Icon(
                        Icons.currency_exchange,
                        color: AppColors.moduleIndigo,
                      ),
                    );
                  },
                ),
              );
            }),
          ),
        ],
      ),
    );
  }
}
