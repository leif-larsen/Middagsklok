import { NextRequest, NextResponse } from "next/server";

function getApiBaseUrl(): string | undefined {
  // Read at runtime, not at module load time
  return process.env.services__api__http__0;
}

async function proxyRequest(request: NextRequest) {
  const apiBaseUrl = getApiBaseUrl();
  
  console.log("=== API Proxy Debug ===");
  console.log("services__api__http__0:", apiBaseUrl);
  
  if (!apiBaseUrl) {
    console.error("API base URL not configured!");
    return NextResponse.json(
      { error: "API base URL not configured" },
      { status: 503 }
    );
  }

  const pathname = request.nextUrl.pathname.replace(/^\/api/, "");
  const url = `${apiBaseUrl}${pathname}${request.nextUrl.search}`;
  
  console.log("Request pathname:", request.nextUrl.pathname);
  console.log("Stripped pathname:", pathname);
  console.log("Final URL:", url);
  console.log("Method:", request.method);
  
  const headers = new Headers();
  headers.set("content-type", request.headers.get("content-type") ?? "application/json");

  // Read body as text instead of streaming
  let body: string | null = null;
  if (request.method !== "GET" && request.method !== "HEAD") {
    body = await request.text();
    console.log("Body length:", body.length);
  }

  try {
    const response = await fetch(url, {
      method: request.method,
      headers,
      body,
    });

    console.log(`API response: ${response.status} ${response.statusText}`);

    const responseHeaders = new Headers(response.headers);
    responseHeaders.delete("transfer-encoding");

    return new NextResponse(response.body, {
      status: response.status,
      statusText: response.statusText,
      headers: responseHeaders,
    });
  } catch (error) {
    console.error("Proxy error:", error);
    console.error("Failed URL was:", url);
    return NextResponse.json(
      { error: "Failed to proxy request", details: String(error), url },
      { status: 502 }
    );
  }
}

export async function GET(request: NextRequest) {
  return proxyRequest(request);
}

export async function POST(request: NextRequest) {
  return proxyRequest(request);
}

export async function PUT(request: NextRequest) {
  return proxyRequest(request);
}

export async function DELETE(request: NextRequest) {
  return proxyRequest(request);
}

export async function PATCH(request: NextRequest) {
  return proxyRequest(request);
}
