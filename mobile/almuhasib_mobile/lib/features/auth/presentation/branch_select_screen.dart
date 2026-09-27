import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../../../core/network/api_exception.dart';
import '../../../shared/models/auth_models.dart';

class BranchSelectScreen extends StatefulWidget {
  const BranchSelectScreen({super.key});

  @override
  State<BranchSelectScreen> createState() => _BranchSelectScreenState();
}

class _BranchSelectScreenState extends State<BranchSelectScreen> {
  final _loading = false.obs;
  final _error = RxnString();
  late final List<BranchInfo> _branches;
  BranchInfo? _selected;

  @override
  void initState() {
    super.initState();
    _branches = AppServices.prefs.allowedBranches;
    _selected = _branches.cast<BranchInfo?>().firstWhere(
          (b) => b?.isDefault == true,
          orElse: () => _branches.isNotEmpty ? _branches.first : null,
        );
  }

  Future<void> _confirm() async {
    if (_selected == null) {
      _error.value = 'اختر فرعاً للمتابعة';
      return;
    }
    _loading.value = true;
    _error.value = null;
    try {
      await AppServices.auth.selectBranch(_selected!.branchId);
      Get.offAllNamed(AppServices.prefs.launchRoute);
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
      appBar: AppBar(title: const Text('اختر الفرع')),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'يجب اختيار فرع قبل عرض بيانات النظام',
              style: Theme.of(context).textTheme.bodyMedium,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 16),
            Expanded(
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
                    subtitle: Text(branch.code),
                    trailing: selected
                        ? const Icon(Icons.check_circle)
                        : const Icon(Icons.circle_outlined),
                    onTap: () => setState(() => _selected = branch),
                  );
                },
              ),
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
                    : const Text('متابعة'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
