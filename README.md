<h1 align="center">afdian-action</h1>

> 🔧 自动更新 爱发电 赞助列表 | GitHub Action

[![repo size](https://img.shields.io/github/repo-size/yiyungent/afdian-action.svg?style=flat)]()
[![LICENSE](https://img.shields.io/github/license/yiyungent/afdian-action.svg?style=flat)](https://github.com/yiyungent/afdian-action/blob/main/LICENSE)


## 介绍

使用 `GitHub Action` 自动更新 爱发电 赞助列表, 无需再手动更新赞助列表。

## 功能

- [x] 自动更新 爱发电 赞助列表
- [x] 支持 Razor 语法, 高度灵活 (编程式自定义模板), 可使用模板文件自定义样式风格

## 使用

### 1. 创建 模板文件 afdian-action.cshtml

> .github/afdian-action.cshtml

> 此模板 可用 Razor 语法, 可自定义    
> PS: 一个更美观完善的 [afdian-action.cshtml](https://github.com/yiyungent/Meting4Net/blob/master/.github/afdian-action.cshtml)

```cshtml
@{
    var viewModel = (Afdian.Action.ViewModels.AfdianViewModel)@Model;
}

@for (int i = 0; i < viewModel.Sponsor.data.list.Count(); i++)
{
    @{ 
        var sponsorItem  = viewModel.Sponsor.data.list[i];
     }

<a href="https://afdian.com/u/@sponsorItem.user.user_id">
    <img src="@sponsorItem.user.avatar?imageView2/1/w/120/h/120" width="40" height="40" alt="@sponsorItem.user.name" title="@sponsorItem.user.name"/>
</a>
}
<!-- 注意: 尽量将标签前靠,否则经测试可能被 GitHub 解析为代码块 -->
```

> 补充: 此模板 风格来自 <https://github.com/CnGal/CnGalWebSite>   
> 如需更多自定义, 见下方 自定义模板

### 2. 修改 目标文件: 如: README.md

> 添加如下 `开始,结束标志`, 此标志中间将插入模板解析后的赞助列表

```markdown
## 赞助者

感谢这些来自爱发电的赞助者：

<!-- AFDIAN-ACTION:START -->
<!-- AFDIAN-ACTION:END -->
```

### 3. 创建 afdian-action.yml

> .github/workflows/afdian-action.yml

```yml
name: afdian-action

on:
  schedule: # Run workflow automatically
    - cron: '0 * * * *' # Runs every hour, on the hour
  workflow_dispatch: # Run workflow manually (without waiting for the cron to be called), through the Github Actions Workflow page directly

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout source
        uses: actions/checkout@v2
        with:
          ref: main # 注意修改为你的分支, 例如: master

      - name: Afdian action
        uses: yiyungent/afdian-action@main
        with:
          # 在 Settings->Secrets 配置 AFDIAN_USERID, AFDIAN_TOKEN
          # 爱发电 user_id
          afdian_userId: ${{ secrets.AFDIAN_USERID }}
          # 爱发电 token
          afdian_token: ${{ secrets.AFDIAN_TOKEN }}
          # 默认为: .github/afdian-action.cshtml
          template_filePath: ".github/afdian-action.cshtml"
          # 默认为: README.md
          target_filePath: "README.md"
          # 可省, 高级选项: RazorEngine Complie, 在 cshtml 中需要 using 的 namespace, 多个用 ; 隔开
          usings: "System"
          # 可省, 高级选项: RazorEngine Complie, 在 cshtml 中需要添加的 系统引用, 多个用 ; 隔开
          # 例如: assemblyReferences: "System.Collections, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
          assemblyReferences: "System.Runtime, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"


      # 下方为 直接 push 到目标分支, 当然你也可以选择 Pull Request 方式
      - name: Commit files
        run: |
          git config --local user.email "41898282+github-actions[bot]@users.noreply.github.com"
          git config --local user.name "github-actions[bot]"
          git commit -m "Add changes: 爱发电赞助" -a
      
      - name: Push changes
        uses: ad-m/github-push-action@master # https://github.com/ad-m/github-push-action
        with:
          github_token: ${{ secrets.GITHUB_TOKEN }}
          branch: main # 注意修改为你的分支, 例如: master

```

> 如果你不想 直接 push, 那么也可以通过 `Pull Request`

```yml
    # 发起 Pull Request
    # Make changes to pull request here
    # https://github.com/peter-evans/create-pull-request
    - name: Create Pull Request
      uses: peter-evans/create-pull-request@v3
      with:
        commit-message: "Update 爱发电 赞助"
        branch: afdian-action/update
        delete-branch: true
        branch-suffix: "short-commit-hash"
        title: '[afdian-action] Update 赞助'
        body: "更新 爱发电 赞助"
        labels: |
          afdian-action
          automated pr
```

> 生成效果见 [Sponsors.md](https://github.com/yiyungent/afdian-action/blob/main/Sponsors.md)


## 自定义模板

> 事实上, `@Model.Order` 值与爱发电官方 `queryOrder` 返回一致, `@Model.Sponsor` 值与爱发电官方 `querySponsor` 返回一致

<details>
  <summary>点我 打开/关闭 模板参数</summary>

```csharp
// AfdianViewModel 即为 @Model
public class AfdianViewModel
{
    public QueryOrderResponseModel Order { get; set; }

    public QuerySponsorResponseModel Sponsor { get; set; }
}
```

```csharp
namespace Afdian.Sdk.ResponseModels
{
    public class QueryOrderResponseModel
    {
        public class DataModel
        {
            public class ListItemModel
            {
                public class SkuDetailItemModel
                {
                    public string sku_id
                    {
                        get;
                        set;
                    }

                    public long count
                    {
                        get;
                        set;
                    }

                    public string name
                    {
                        get;
                        set;
                    }

                    public string album_id
                    {
                        get;
                        set;
                    }

                    public string pic
                    {
                        get;
                        set;
                    }
                }

                //
                // Summary:
                //     订单号
                public string out_trade_no
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     下单用户ID
                public string user_id
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     方案ID，如自选，则为空
                public string plan_id
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     订单描述
                public string title
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     赞助月份
                public int month
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     真实付款金额，如有兑换码，则为0.00
                public string total_amount
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     显示金额，如有折扣则为折扣前金额
                public string show_amount
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     2 为交易成功。目前仅会推送此类型
                public int status
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     订单留言
                public string remark
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     兑换码ID
                public string redeem_id
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     0表示常规方案 1表示售卖方案
                public int product_type
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     折扣
                public string discount
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     如果为售卖类型，以数组形式表示具体型号
                public List<SkuDetailItemModel> sku_detail
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     收件人
                public string address_person
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     收件人电话
                public string address_phone
                {
                    get;
                    set;
                }

                //
                // Summary:
                //     收件人地址
                public string address_address
                {
                    get;
                    set;
                }
            }

            public List<ListItemModel> list
            {
                get;
                set;
            }

            public int total_count
            {
                get;
                set;
            }

            public int total_page
            {
                get;
                set;
            }
        }

        //
        // Summary:
        //     ec 为 200 时，表示请求正常，否则 异常，同时 em 会提示错误信息 400001 params incomplete 400002 time was
        //     expired 400003 params was not valid json string 400004 no valid token found 400005
        //     sign validation failed 响应 400005 时，会 data.debug 处返回服务端对参数做拼接的结构
        public int ec
        {
            get;
            set;
        }

        public string em
        {
            get;
            set;
        }

        public DataModel data
        {
            get;
            set;
        }
    }
}
```

```csharp
namespace Afdian.Sdk.ResponseModels
{
    public class QuerySponsorResponseModel
    {
        public class DataModel
        {
            public class ListItemModel
            {
                public class SponsorPlanModel
                {
                    public string plan_id
                    {
                        get;
                        set;
                    }

                    public int rank
                    {
                        get;
                        set;
                    }

                    public string user_id
                    {
                        get;
                        set;
                    }

                    public int status
                    {
                        get;
                        set;
                    }

                    public string name
                    {
                        get;
                        set;
                    }

                    public string pic
                    {
                        get;
                        set;
                    }

                    public string desc
                    {
                        get;
                        set;
                    }

                    public string price
                    {
                        get;
                        set;
                    }

                    public int update_time
                    {
                        get;
                        set;
                    }

                    public int pay_month
                    {
                        get;
                        set;
                    }

                    public string show_price
                    {
                        get;
                        set;
                    }

                    public int independent
                    {
                        get;
                        set;
                    }

                    public int permanent
                    {
                        get;
                        set;
                    }

                    public int can_buy_hide
                    {
                        get;
                        set;
                    }

                    public int need_address
                    {
                        get;
                        set;
                    }

                    public int product_type
                    {
                        get;
                        set;
                    }

                    public int sale_limit_count
                    {
                        get;
                        set;
                    }

                    public bool need_invite_code
                    {
                        get;
                        set;
                    }

                    public int expire_time
                    {
                        get;
                        set;
                    }

                    public List<object> sku_processed
                    {
                        get;
                        set;
                    }

                    public int rankType
                    {
                        get;
                        set;
                    }
                }

                public class CurrentPlanModel
                {
                    public string plan_id
                    {
                        get;
                        set;
                    }

                    public int rank
                    {
                        get;
                        set;
                    }

                    public string user_id
                    {
                        get;
                        set;
                    }

                    public int status
                    {
                        get;
                        set;
                    }

                    public string name
                    {
                        get;
                        set;
                    }

                    public string pic
                    {
                        get;
                        set;
                    }

                    public string desc
                    {
                        get;
                        set;
                    }

                    public string price
                    {
                        get;
                        set;
                    }

                    public int update_time
                    {
                        get;
                        set;
                    }

                    public int pay_month
                    {
                        get;
                        set;
                    }

                    public string show_price
                    {
                        get;
                        set;
                    }

                    public int independent
                    {
                        get;
                        set;
                    }

                    public int permanent
                    {
                        get;
                        set;
                    }

                    public int can_buy_hide
                    {
                        get;
                        set;
                    }

                    public int need_address
                    {
                        get;
                        set;
                    }

                    public int product_type
                    {
                        get;
                        set;
                    }

                    public int sale_limit_count
                    {
                        get;
                        set;
                    }

                    public bool need_invite_code
                    {
                        get;
                        set;
                    }

                    public int expire_time
                    {
                        get;
                        set;
                    }

                    public List<object> sku_processed
                    {
                        get;
                        set;
                    }

                    public int rankType
                    {
                        get;
                        set;
                    }
                }

                public class UserModel
                {
                    public string user_id
                    {
                        get;
                        set;
                    }

                    public string name
                    {
                        get;
                        set;
                    }

                    public string avatar
                    {
                        get;
                        set;
                    }
                }

                public List<SponsorPlanModel> sponsor_plans
                {
                    get;
                    set;
                }

                public CurrentPlanModel current_plan
                {
                    get;
                    set;
                }

                public string all_sum_amount
                {
                    get;
                    set;
                }

                public long create_time
                {
                    get;
                    set;
                }

                public long last_pay_time
                {
                    get;
                    set;
                }

                public UserModel user
                {
                    get;
                    set;
                }
            }

            public List<ListItemModel> list
            {
                get;
                set;
            }

            public int total_count
            {
                get;
                set;
            }

            public int total_page
            {
                get;
                set;
            }
        }

        //
        // Summary:
        //     ec 为 200 时，表示请求正常，否则 异常，同时 em 会提示错误信息 400001 params incomplete 400002 time was
        //     expired 400003 params was not valid json string 400004 no valid token found 400005
        //     sign validation failed 响应 400005 时，会 data.debug 处返回服务端对参数做拼接的结构
        public int ec
        {
            get;
            set;
        }

        public string em
        {
            get;
            set;
        }

        public DataModel data
        {
            get;
            set;
        }
    }
}
```
</details>

## Related Projects

- [yiyungent/Afdian.Sdk: 🍰 爱发电 非官方 .NET SDK](https://github.com/yiyungent/Afdian.Sdk)    

## Donate

afdian-action is an MIT licensed open source project and completely free to use. However, the amount of effort needed to maintain and develop new features for the project is not sustainable without proper financial backing.

We accept donations through these channels:
- <a href="https://afdian.com/@yiyun" target="_blank">爱发电</a>

## Author

**afdian-action** © [yiyun](https://github.com/yiyungent), Released under the [MIT](./LICENSE) License.<br>
Authored and maintained by yiyun with help from contributors ([list](https://github.com/yiyungent/afdian-action/contributors)).

> GitHub [@yiyungent](https://github.com/yiyungent) Gitee [@yiyungent](https://gitee.com/yiyungent)


