import { NextResponse } from "next/server";
import { prisma } from "@/lib/prisma";

// 매 호출마다 실제 DB 쿼리가 나가야 Supabase keep-alive 의미가 있으므로 정적 캐시를 끈다.
export const dynamic = "force-dynamic";

export async function GET() {
  try {
    // topics 테이블 count 1회로 실제 DB 연결을 건드린다. 사용자 데이터는 조회하지 않는다.
    await prisma.topic.count();
    return NextResponse.json(
      {
        status: "ok",
        db: "connected",
        timestamp: new Date().toISOString(),
      },
      { status: 200 },
    );
  } catch (error) {
    // 내부 상세(에러 메시지·스택·접속 정보)는 응답에 절대 포함하지 않고 서버 로그에만 남긴다.
    console.error("[health] DB check failed:", error);
    return NextResponse.json(
      {
        status: "error",
        db: "disconnected",
      },
      { status: 503 },
    );
  }
}
