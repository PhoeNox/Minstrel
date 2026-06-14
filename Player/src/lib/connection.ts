interface ConnectionInfo {
	remoteUrl: string;
}

export async function fetchRemoteUrl(): Promise<string> {
	const response = await fetch('/connection');
	const info = (await response.json()) as ConnectionInfo;
	return info.remoteUrl;
}
