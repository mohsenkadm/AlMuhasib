import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/router/app_routes.dart';
import '../../../shared/utils/formatters.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../controllers/car_contracts_controller.dart';

class CarContractsScreen extends StatelessWidget {
  const CarContractsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final controller =
        Get.put(CarContractsController(), tag: 'car_contracts');

    return Scaffold(
      appBar: AppBar(title: Text('car_contracts_title'.tr())),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => Get.toNamed(AppRoutes.carContractNew),
        icon: const Icon(Icons.add),
        label: Text('car_new_contract'.tr()),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 0),
            child: TextField(
              controller: controller.searchController,
              decoration: InputDecoration(
                hintText: 'search'.tr(),
                prefixIcon: const Icon(Icons.search),
                suffixIcon: IconButton(
                  icon: const Icon(Icons.refresh),
                  onPressed: controller.load,
                ),
              ),
              onSubmitted: (_) => controller.load(),
            ),
          ),
          Expanded(
            child: Obx(() {
              if (controller.isLoading.value) {
                return const Center(child: CircularProgressIndicator());
              }
              if (controller.error.value != null) {
                return ErrorStateWidget(
                  message: 'error_load'.tr(),
                  onRetry: controller.load,
                );
              }
              if (controller.items.isEmpty) {
                return EmptyStateWidget(
                  message: 'car_no_contracts'.tr(),
                  icon: Icons.description_outlined,
                );
              }
              return RefreshIndicator(
                onRefresh: controller.load,
                child: ListView.builder(
                  padding: const EdgeInsets.fromLTRB(20, 12, 20, 120),
                  itemCount: controller.items.length,
                  itemBuilder: (context, i) {
                    final c = controller.items[i];
                    return Padding(
                      padding: const EdgeInsets.only(bottom: 10),
                      child: GradientCard(
                        child: ListTile(
                          contentPadding: EdgeInsets.zero,
                          title: Text(c.contractNumber),
                          subtitle: Text(
                            '${c.buyerName} • ${c.plateNumber}',
                          ),
                          trailing: Column(
                            mainAxisAlignment: MainAxisAlignment.center,
                            crossAxisAlignment: CrossAxisAlignment.end,
                            children: [
                              Text(formatCurrency(c.carPrice)),
                              Text(
                                c.status,
                                style: TextStyle(
                                  fontSize: 12,
                                  color: c.remainingAmount > 0
                                      ? Colors.orange
                                      : Colors.green,
                                ),
                              ),
                            ],
                          ),
                          onTap: () => Get.toNamed(
                            AppRoutes.carContractDetailPath(c.syncId),
                          ),
                        ),
                      ).fadeSlideInList(index: i),
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
