<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    public function up(): void
    {
        Schema::create('role_user', function (Blueprint $table) {
            $table->unsignedBigInteger('user_id');
            $table->unsignedBigInteger('role_id');

            $table->timestamps();

            // Composite primary key
            $table->primary(['user_id', 'role_id']);

            // Indexes
            $table->index('user_id', 'idx_role_user_user_id');
            $table->index('role_id', 'idx_role_user_role_id');

            // FKs
            $table->foreign('user_id', 'role_user_user_fk')
                ->references('id')->on('users')
                ->cascadeOnDelete();

            $table->foreign('role_id', 'role_user_role_fk')
                ->references('id')->on('roles')
                ->cascadeOnDelete();
        });
    }

    public function down(): void
    {
        Schema::table('role_user', function (Blueprint $table) {
            $table->dropForeign('role_user_user_fk');
            $table->dropForeign('role_user_role_fk');
        });

        Schema::dropIfExists('role_user');
    }
};
