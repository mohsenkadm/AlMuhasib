import 'package:flutter/material.dart';

import '../../core/constants/app_colors.dart';

class EntityListTile extends StatelessWidget {
  const EntityListTile({
    super.key,
    required this.name,
    this.subtitle,
    this.trailing,
    this.badge,
    this.onTap,
  });

  final String name;
  final String? subtitle;
  final Widget? trailing;
  final String? badge;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        onTap: onTap,
        leading: CircleAvatar(
          backgroundColor: AppColors.primaryLight.withValues(alpha: 0.15),
          child: Text(
            name.isNotEmpty ? name[0].toUpperCase() : '?',
            style: const TextStyle(color: AppColors.primary),
          ),
        ),
        title: Text(name),
        subtitle: subtitle != null ? Text(subtitle!) : null,
        trailing: trailing ??
            (badge != null
                ? Chip(
                    label: Text(badge!),
                    visualDensity: VisualDensity.compact,
                  )
                : null),
      ),
    );
  }
}
