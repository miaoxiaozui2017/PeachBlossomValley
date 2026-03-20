一、 给AI Agent的开发规范文档

文件名： 01_AI_DEV_GUIDELINES.md

位置： 项目根目录

目标： AI在编写任何代码前必须阅读此文档
------

AI 开发规范 v1.0

基本原则

1. 你只实现函数内部逻辑，不修改架构

2. 遵循现有代码风格

3. 每个函数不超过50行

4. 必须有错误处理

5. 禁止硬编码数值

代码生成格式

模板：简单函数

// [你的注释：功能描述]
public ReturnType FunctionName(Parameters)
{
    // AI实现区域开始
    // 步骤1：...
    // 步骤2：...
    // 返回结果
    // AI实现区域结束
}


模板：数据类

[System.Serializable]
public class ClassName
{
    public Type field;
    
    // AI实现：ToString方法
    public override string ToString()
    {
        // AI实现区域
    }
}


命名规范

• 私有字段：_camelCase

• 局部变量：camelCase

• 常量：UPPER_SNAKE_CASE

• 布尔变量：isXxx/hasXxx/canXxx

禁止行为

❌ 不要修改函数签名

❌ 不要添加新的public方法

❌ 不要删除已有代码

❌ 不要引入新依赖

❌ 不要使用魔数（Magic Numbers）

必须包含

✅ XML注释

✅ 空值检查

✅ 错误日志

✅ 返回值验证