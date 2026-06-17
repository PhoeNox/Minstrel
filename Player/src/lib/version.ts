interface VersionInfo {
	version: string;
}

export async function fetchVersion(): Promise<string> {
	const response = await fetch('/system/version');
	const info = (await response.json()) as VersionInfo;
	return info.version;
}
