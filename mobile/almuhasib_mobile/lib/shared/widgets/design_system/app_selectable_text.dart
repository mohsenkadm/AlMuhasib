import 'package:flutter/material.dart';

/// نص قابل للتحديد والنسخ — بديل SelectableText مع أنماط Theme.
class AppSelectableText extends StatelessWidget {
  const AppSelectableText(
    this.text, {
    super.key,
    this.style,
    this.textAlign,
    this.maxLines,
    this.overflow,
  });

  final String text;
  final TextStyle? style;
  final TextAlign? textAlign;
  final int? maxLines;
  final TextOverflow? overflow;

  @override
  Widget build(BuildContext context) {
    return SelectableText(
      text,
      style: style,
      textAlign: textAlign,
      maxLines: maxLines,
    );
  }
}
