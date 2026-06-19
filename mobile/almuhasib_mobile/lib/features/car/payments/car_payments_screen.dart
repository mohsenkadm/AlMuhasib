import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/router/app_routes.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../controllers/car_payments_controller.dart';

class CarPaymentsScreen extends StatelessWidget {
  const CarPaymentsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final controller =
        Get.put(CarPaymentsController(), tag: 'car_payments');

    return Scaffold(
      appBar: AppBar(title: Text('car_payments_title'.tr())),
      body: Obx(() {
        if (controller.isLoading.value) {
          return const Center(child: CircularProgressIndicator());
        }
        if (controller.unpaid.isEmpty) {
          return EmptyStateWidget(
            message: 'car_no_unpaid'.tr(),
            icon: Icons.payments_outlined,
          );
        }
        return RefreshIndicator(
          onRefresh: controller.load,
          child: ListView.builder(
            padding: const EdgeInsets.fromLTRB(20, 12, 20, 120),
            itemCount: controller.unpaid.length,
            itemBuilder: (context, i) {
              final c = controller.unpaid[i];
              return Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: GradientCard(
                  child: ListTile(
                    title: Text(c.contractNumber),
                    subtitle: Text(c.buyerName),
                    trailing: Text('${c.remainingAmount}'),
                    onTap: () => Get.toNamed(
                      AppRoutes.carContractDetailPath(c.syncId),
                    ),
                  ),
                ),
              );
            },
          ),
        );
      }),
    );
  }
}
