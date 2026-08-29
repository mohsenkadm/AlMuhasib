import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../models/gold_shop_models.dart';

class GoldStockController extends GetxController {
  final items = <GoldStockRow>[].obs;
  final isLoading = false.obs;
  final error = ''.obs;

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = '';
    try {
      items.value = await AppServices.goldShop.getStock();
    } catch (e) {
      error.value = e.toString();
    } finally {
      isLoading.value = false;
    }
  }
}

class GoldStockScreen extends StatelessWidget {
  const GoldStockScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final c = Get.put(GoldStockController());
    return Scaffold(
      appBar: AppBar(
        title: const Text('مخزون الذهب'),
        actions: [
          IconButton(onPressed: c.load, icon: const Icon(Icons.refresh)),
        ],
      ),
      body: Obx(() {
        if (c.isLoading.value && c.items.isEmpty) {
          return const Center(child: CircularProgressIndicator());
        }
        if (c.error.value.isNotEmpty && c.items.isEmpty) {
          return Center(child: Text(c.error.value));
        }
        if (c.items.isEmpty) {
          return const Center(child: Text('لا يوجد مخزون'));
        }
        return RefreshIndicator(
          onRefresh: c.load,
          child: ListView.separated(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
            itemCount: c.items.length,
            separatorBuilder: (_, __) => const SizedBox(height: 8),
            itemBuilder: (_, i) {
              final row = c.items[i];
              return Card(
                child: ListTile(
                  leading: CircleAvatar(
                    child: Text('${row.karatValue}'),
                  ),
                  title: Text(row.karatName.isEmpty
                      ? 'عيار ${row.karatValue}'
                      : row.karatName),
                  subtitle: Text(
                    '${row.gramsOnHand.toStringAsFixed(3)} غ'
                    '${row.isLowStock ? ' — مخزون منخفض' : ''}',
                  ),
                  trailing: Text(
                    row.stockValue.toStringAsFixed(0),
                    style: const TextStyle(fontWeight: FontWeight.w600),
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

class GoldSuppliersController extends GetxController {
  final items = <GoldSupplierItem>[].obs;
  final isLoading = false.obs;
  final error = ''.obs;
  final search = ''.obs;

  @override
  void onInit() {
    super.onInit();
    load();
  }

  Future<void> load() async {
    isLoading.value = true;
    error.value = '';
    try {
      items.value =
          await AppServices.goldShop.getSuppliers(search: search.value);
    } catch (e) {
      error.value = e.toString();
    } finally {
      isLoading.value = false;
    }
  }
}

class GoldSuppliersScreen extends StatelessWidget {
  const GoldSuppliersScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final c = Get.put(GoldSuppliersController());
    return Scaffold(
      appBar: AppBar(
        title: const Text('الموردون'),
        actions: [
          IconButton(onPressed: c.load, icon: const Icon(Icons.refresh)),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 8),
            child: TextField(
              decoration: const InputDecoration(
                hintText: 'بحث عن مورد',
                prefixIcon: Icon(Icons.search),
                border: OutlineInputBorder(),
              ),
              onChanged: (v) {
                c.search.value = v;
                c.load();
              },
            ),
          ),
          Expanded(
            child: Obx(() {
              if (c.isLoading.value && c.items.isEmpty) {
                return const Center(child: CircularProgressIndicator());
              }
              if (c.error.value.isNotEmpty && c.items.isEmpty) {
                return Center(child: Text(c.error.value));
              }
              if (c.items.isEmpty) {
                return const Center(child: Text('لا يوجد موردون'));
              }
              return RefreshIndicator(
                onRefresh: c.load,
                child: ListView.separated(
                  padding: const EdgeInsets.fromLTRB(16, 0, 16, 24),
                  itemCount: c.items.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 8),
                  itemBuilder: (_, i) {
                    final s = c.items[i];
                    return Card(
                      child: ListTile(
                        title: Text(s.name),
                        subtitle: Text(
                          [
                            if (s.phone.isNotEmpty) s.phone,
                            if (s.creditBalanceIqd > 0)
                              'آجل د.ع ${s.creditBalanceIqd.toStringAsFixed(0)}',
                            if (s.creditBalanceUsd > 0)
                              'آجل \$ ${s.creditBalanceUsd.toStringAsFixed(2)}',
                          ].join(' · '),
                        ),
                        trailing: const Icon(Icons.chevron_left),
                        onTap: () => Get.toNamed(
                          AppRoutes.goldShopSupplierStatementPath(
                            s.id,
                            name: s.name,
                          ),
                        ),
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
