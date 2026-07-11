import { PrismaClient } from "@/src/generated/prisma";
import { PrismaPg } from "@prisma/adapter-pg";

// Prisma 7은 드라이버 어댑터를 필수로 요구한다. Supabase PostgreSQL에는 pg 어댑터를 쓴다.
// 접속 정보는 process.env.DATABASE_URL 참조로만 사용하고, 값을 코드에 넣지 않는다.
const globalForPrisma = globalThis as unknown as { prisma?: PrismaClient };

function createPrismaClient(): PrismaClient {
  const adapter = new PrismaPg({ connectionString: process.env.DATABASE_URL });
  return new PrismaClient({ adapter });
}

// Next.js 개발 모드의 hot reload로 인스턴스가 중복 생성되지 않도록 globalThis에 캐시한다.
export const prisma = globalForPrisma.prisma ?? createPrismaClient();

if (process.env.NODE_ENV !== "production") {
  globalForPrisma.prisma = prisma;
}
