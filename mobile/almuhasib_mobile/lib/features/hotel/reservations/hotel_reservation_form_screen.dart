import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../shared/widgets/common_widgets.dart';
import '../controllers/hotel_reservation_form_controller.dart';
import '../models/hotel_models.dart';

class HotelReservationFormScreen extends GetView<HotelReservationFormController> {
  const HotelReservationFormScreen({super.key});

  @override
  final String? tag = 'hotel_reservation_form';

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('hotel_new_reservation'.tr())),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          GradientCard(
            child: Column(
              children: [
                TextField(
                  controller: controller.guestNameController,
                  decoration: InputDecoration(labelText: 'hotel_guest'.tr()),
                ),
                const SizedBox(height: 12),
                Obx(
                  () => DropdownButtonFormField<HotelRoom>(
                    value: controller.selectedRoom.value,
                    decoration: InputDecoration(labelText: 'hotel_room'.tr()),
                    items: controller.rooms
                        .map(
                          (room) => DropdownMenuItem(
                            value: room,
                            child: Text(room.roomNumber),
                          ),
                        )
                        .toList(),
                    onChanged: controller.selectRoom,
                  ),
                ),
                const SizedBox(height: 12),
                Obx(
                  () => ListTile(
                    title: Text('hotel_check_in'.tr()),
                    subtitle: Text(
                      controller.checkIn.value.toString().split(' ').first,
                    ),
                    onTap: () => controller.pickCheckIn(context),
                  ),
                ),
                Obx(
                  () => ListTile(
                    title: Text('hotel_check_out'.tr()),
                    subtitle: Text(
                      controller.checkOut.value.toString().split(' ').first,
                    ),
                    onTap: () => controller.pickCheckOut(context),
                  ),
                ),
                const SizedBox(height: 12),
                TextField(
                  controller: controller.totalAmountController,
                  keyboardType: TextInputType.number,
                  decoration: InputDecoration(
                    labelText: 'net_amount'.tr(),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 20),
          Obx(
            () => FilledButton(
              onPressed: controller.saving.value ? null : controller.save,
              child: controller.saving.value
                  ? const SizedBox(
                      width: 22,
                      height: 22,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : Text('save'.tr()),
            ),
          ),
        ],
      ),
    );
  }
}
