import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../controllers/car_report_controller.dart';

class CarReportScreen extends StatelessWidget {
  const CarReportScreen({super.key});

  Future<void> _pickFrom(
    BuildContext context,
    CarReportController controller,
  ) async {
    final d = await showDatePicker(
      context: context,
      initialDate: controller.from.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (d != null) controller.setFrom(d);
  }

  Future<void> _pickTo(
    BuildContext context,
    CarReportController controller,
  ) async {
    final d = await showDatePicker(
      context: context,
      initialDate: controller.to.value,
      firstDate: DateTime(2020),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (d != null) controller.setTo(d);
  }

  @override
  Widget build(BuildContext context) {
    final controller = Get.put(CarReportController(), tag: 'car_report');

    return Scaffold(
      appBar: AppBar(title: Text('car_report_title'.tr())),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(20),
            child: Obx(
              () => Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () => _pickFrom(context, controller),
                      child: Text(formatDate(controller.from.value)),
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: OutlinedButton(
                      onPressed: () => _pickTo(context, controller),
                      child: Text(formatDate(controller.to.value)),
                    ),
                  ),
                ],
              ),
            ),
          ),
          Obx(() {
            if (controller.isLoading.value) {
              return const SizedBox.shrink();
            }
            return Padding(
              padding: const EdgeInsets.symmetric(horizontal: 20),
              child: GradientCard(
                child: ListTile(
                  title: Text('car_report_total'.tr()),
                  trailing: Text(formatCurrency(controller.total)),
                ),
              ),
            );
          }),
          Expanded(
            child: Obx(() {
              if (controller.isLoading.value) {
                return const Center(child: CircularProgressIndicator());
              }
              return ListView.builder(
                padding: const EdgeInsets.fromLTRB(20, 12, 20, 120),
                itemCount: controller.rows.length,
                itemBuilder: (context, i) {
                  final r = controller.rows[i];
                  return Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: GradientCard(
                      child: ListTile(
                        title: Text(r.contractNumber),
                        subtitle: Text(formatDate(r.contractDate)),
                        trailing: Text(formatCurrency(r.carPrice)),
                      ),
                    ),
                  );
                },
              );
            }),
          ),
        ],
      ),
    );
  }
}
