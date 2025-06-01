# DandanCache

- 每天定时从bangumi获取最近一周的新番信息
- 根据弹弹play的缓存建议设定两个弹幕更新的数据库(预计有第三个，旧动画库，每个月更新一次)
    - 当天和前一天更新的动画，设为热更新库，每小时更新一次弹幕
    - 其他一周内的动画，为冷更新库，每天更新一次弹幕

## 依赖

- [bangumi archive](https://github.com/bangumi/Archive)
- [bangumi-data](https://github.com/bangumi-data/bangumi-data)
- [bilibili api](https://github.com/SocialSisterYi/bilibili-API-collect)