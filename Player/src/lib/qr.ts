import qrcode from 'qrcode-generator';

export function qrSvg(text: string): string {
	const qr = qrcode(0, 'M');
	qr.addData(text);
	qr.make();
	return qr.createSvgTag({ scalable: true });
}
