-- CreateTable
CREATE TABLE "profiles" (
    "id" UUID NOT NULL,
    "username" TEXT,
    "grade_code" TEXT,
    "created_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "updated_at" TIMESTAMP(3) NOT NULL,

    CONSTRAINT "profiles_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "topics" (
    "id" UUID NOT NULL,
    "stage" INTEGER NOT NULL,
    "unit_code" TEXT NOT NULL,
    "title" TEXT NOT NULL,
    "created_at" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "topics_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE UNIQUE INDEX "topics_unit_code_key" ON "topics"("unit_code");
