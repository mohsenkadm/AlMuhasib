import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/constants/app_colors.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../../shared/widgets/common_widgets.dart';
import '../../../shared/widgets/design_system/design_system.dart';
import '../../../shared/widgets/shimmer_widgets.dart';
import '../controllers/hotel_rooms_controller.dart';
import '../models/hotel_models.dart';
import '../models/hotel_status_helpers.dart';

class HotelRoomsScreen extends GetView<HotelRoomsController> {
  const HotelRoomsScreen({super.key});

  @override
  final String? tag = 'hotel_rooms';

  @override
  Widget build(BuildContext context) {
    return AppPageScaffold(
      title: 'hotel_rooms_title'.tr(),
      subtitle: 'hotel_rooms_subtitle'.tr(),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 8, 20, 0),
            child: _StatusLegend(controller: controller).fadeSlideIn(),
          ),
          Expanded(
            child: Obx(() {
              final rooms = controller.rooms.value;
              final isLoading = controller.isLoading.value;
              final error = controller.error.value;

              if (rooms.isNotEmpty) {
                return RefreshIndicator(
                  onRefresh: controller.load,
                  child: GridView.builder(
                    physics: const AlwaysScrollableScrollPhysics(),
                    padding: const EdgeInsets.fromLTRB(16, 16, 16, 120),
                    gridDelegate:
                        const SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 3,
                      mainAxisSpacing: 12,
                      crossAxisSpacing: 12,
                      childAspectRatio: 0.85,
                    ),
                    itemCount: rooms.length,
                    itemBuilder: (context, index) {
                      return _RoomCard(room: rooms[index])
                          .fadeSlideInList(index: index);
                    },
                  ),
                );
              }

              if (isLoading) return const ListShimmer();

              if (error != null) {
                return RefreshIndicator(
                  onRefresh: controller.load,
                  child: ListView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    children: [
                      SizedBox(
                        height: MediaQuery.sizeOf(context).height * 0.45,
                        child: ErrorStateWidget(
                          message: AppExceptionHandler.messageFor(error),
                          onRetry: controller.load,
                        ),
                      ),
                    ],
                  ),
                );
              }

              return RefreshIndicator(
                onRefresh: controller.load,
                child: ListView(
                  physics: const AlwaysScrollableScrollPhysics(),
                  children: [
                    SizedBox(
                      height: MediaQuery.sizeOf(context).height * 0.35,
                      child: EmptyStateWidget(message: 'no_data'.tr()),
                    ),
                  ],
                ),
              );
            }),
          ),
        ],
      ),
    );
  }
}

class _RoomCard extends StatelessWidget {
  const _RoomCard({required this.room});

  final HotelRoom room;

  @override
  Widget build(BuildContext context) {
    final color = hotelRoomStatusColor(room.status);

    return Card(
      elevation: 0,
      clipBehavior: Clip.antiAlias,
      child: Container(
        decoration: BoxDecoration(
          border: Border.all(color: color.withValues(alpha: 0.45), width: 1.5),
          borderRadius: BorderRadius.circular(AppColors.cardRadius),
        ),
        padding: const EdgeInsets.all(10),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(Icons.meeting_room_rounded, color: color, size: 28),
            const SizedBox(height: 8),
            Text(
              room.roomNumber,
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.w800,
                  ),
              maxLines: 1,
              overflow: TextOverflow.ellipsis,
            ),
            if (room.roomTypeName != null) ...[
              const SizedBox(height: 4),
              Text(
                room.roomTypeName!,
                style: Theme.of(context).textTheme.bodySmall,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                textAlign: TextAlign.center,
              ),
            ],
            const SizedBox(height: 6),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
              decoration: BoxDecoration(
                color: color.withValues(alpha: 0.15),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(
                hotelRoomStatusLabel(room.status),
                style: TextStyle(
                  color: color,
                  fontSize: 10,
                  fontWeight: FontWeight.w600,
                ),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _StatusLegend extends StatelessWidget {
  const _StatusLegend({required this.controller});

  final HotelRoomsController controller;

  @override
  Widget build(BuildContext context) {
    final statuses = [
      HotelRoomStatus.available,
      HotelRoomStatus.occupied,
      HotelRoomStatus.dirty,
      HotelRoomStatus.maintenance,
    ];

    return Obx(() {
      final selected = controller.statusFilter.value;
      return SizedBox(
        height: 40,
        child: ListView.separated(
          scrollDirection: Axis.horizontal,
          itemCount: statuses.length + 1,
          separatorBuilder: (_, __) => const SizedBox(width: 8),
          itemBuilder: (context, index) {
            if (index == 0) {
              final isAll = selected == null;
              return FilterChip(
                label: Text('filter_all'.tr()),
                selected: isAll,
                onSelected: (_) => controller.clearStatusFilter(),
              );
            }
            final status = statuses[index - 1];
            final color = hotelRoomStatusColor(status);
            final isSelected = selected == status;
            return FilterChip(
              avatar: CircleAvatar(backgroundColor: color, radius: 6),
              label: Text(
                hotelRoomStatusLabel(status),
                style: const TextStyle(fontSize: 11),
              ),
              selected: isSelected,
              onSelected: (_) => controller.updateStatusFilter(
                isSelected ? null : status,
              ),
            );
          },
        ),
      );
    });
  }
}
